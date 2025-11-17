package main

/*
#include <stdlib.h>
*/
import "C"
import (
	"encoding/json"
	"fmt"
	"net/url"
	"sync"
	"time"
	"unsafe"

	"github.com/nats-io/jwt/v2"
	"github.com/nats-io/nats-server/v2/server"
	"github.com/nats-io/nkeys"
)

var (
	// Map of server instances by port (supports multiple servers per process)
	natsServers = make(map[int]*server.Server)
	currentPort int // Most recently started server port (for GetServerInfo/GetClientURL)
	serverMu    sync.Mutex
)

// ServerConfig represents the configuration for the NATS server
type ServerConfig struct {
	Host               string         `json:"host"`
	Port               int            `json:"port"`
	MaxPayload         int            `json:"max_payload"`
	MaxControlLine     int            `json:"max_control_line"`
	PingInterval       int            `json:"ping_interval"`
	MaxPingsOut        int            `json:"max_pings_out"`
	WriteDeadline      int            `json:"write_deadline"`
	Debug              bool           `json:"debug"`
	Trace              bool           `json:"trace"`
	LogFile            string         `json:"log_file"`
	LogTimeUtc         bool           `json:"log_time_utc"`
	LogFileSize        int64          `json:"log_file_size"`
	Jetstream          bool           `json:"jetstream"`
	JetstreamStoreDir  string         `json:"jetstream_store_dir"`
	JetstreamMaxMemory int64          `json:"jetstream_max_memory"`
	JetstreamMaxStore  int64          `json:"jetstream_max_store"`
	JetstreamDomain    string         `json:"jetstream_domain"`
	JetstreamUniqueTag string         `json:"jetstream_unique_tag"`
	HTTPPort           int            `json:"http_port"`
	HTTPHost           string         `json:"http_host"`
	HTTPSPort          int            `json:"https_port"`
	Auth               AuthConfig     `json:"auth"`
	LeafNode           LeafNodeConfig `json:"leaf_node"`
	Cluster            ClusterConfig  `json:"cluster"`
}

type AuthConfig struct {
	Username     string   `json:"username"`
	Password     string   `json:"password"`
	Token        string   `json:"token"`
	AllowedUsers []string `json:"allowed_users"`
}

type LeafNodeConfig struct {
	Host           string   `json:"host"`
	Port           int      `json:"port"`
	RemoteURLs     []string `json:"remote_urls"`
	AuthUsername   string   `json:"auth_username"`
	AuthPassword   string   `json:"auth_password"`
	TLSCert        string   `json:"tls_cert"`
	TLSKey         string   `json:"tls_key"`
	TLSCACert      string   `json:"tls_ca_cert"`
	ImportSubjects []string `json:"import_subjects"`
	ExportSubjects []string `json:"export_subjects"`
}

type ClusterConfig struct {
	Name           string   `json:"name"`
	Host           string   `json:"host"`
	Port           int      `json:"port"`
	Routes         []string `json:"routes"`
	AuthUsername   string   `json:"auth_username"`
	AuthPassword   string   `json:"auth_password"`
	AuthToken      string   `json:"auth_token"`
	ConnectTimeout int      `json:"connect_timeout"`
	TLSCert        string   `json:"tls_cert"`
	TLSKey         string   `json:"tls_key"`
	TLSCACert      string   `json:"tls_ca_cert"`
	TLSVerify      bool     `json:"tls_verify"`
}

type AccountConfig struct {
	Name             string `json:"name"`
	Description      string `json:"description"`
	MaxConnections   int    `json:"max_connections"`
	MaxSubscriptions int    `json:"max_subscriptions"`
	MaxData          int64  `json:"max_data"`
	MaxPayload       int64  `json:"max_payload"`
}

// convertToNatsOptions converts our config to NATS server options
func convertToNatsOptions(config *ServerConfig) *server.Options {
	opts := &server.Options{
		Host:           config.Host,
		Port:           config.Port,
		MaxPayload:     int32(config.MaxPayload),
		MaxControlLine: int32(config.MaxControlLine),
		PingInterval:   server.DEFAULT_PING_INTERVAL,
		MaxPingsOut:    config.MaxPingsOut,
		WriteDeadline:  server.DEFAULT_FLUSH_DEADLINE,
		Debug:          config.Debug,
		Trace:          config.Trace,
		Logtime:        config.LogTimeUtc,
		HTTPHost:       config.HTTPHost,
		HTTPPort:       config.HTTPPort,
		HTTPSPort:      config.HTTPSPort,
		LogFile:        config.LogFile,
		LogSizeLimit:   config.LogFileSize,
	}

	// Configure authentication if provided
	if config.Auth.Username != "" && config.Auth.Password != "" {
		opts.Username = config.Auth.Username
		opts.Password = config.Auth.Password
	} else if config.Auth.Token != "" {
		opts.Authorization = config.Auth.Token
	}

	// Configure JetStream if enabled
	if config.Jetstream {
		opts.JetStream = true
		opts.StoreDir = config.JetstreamStoreDir
		if config.JetstreamMaxMemory > 0 {
			opts.JetStreamMaxMemory = config.JetstreamMaxMemory
		}
		if config.JetstreamMaxStore > 0 {
			opts.JetStreamMaxStore = config.JetstreamMaxStore
		}
		if config.JetstreamDomain != "" {
			opts.JetStreamDomain = config.JetstreamDomain
		}
		if config.JetstreamUniqueTag != "" {
			opts.JetStreamUniqueTag = config.JetstreamUniqueTag
		}
	}

	// Configure Leaf Node if port is set
	if config.LeafNode.Port > 0 {
		opts.LeafNode.Host = config.LeafNode.Host
		opts.LeafNode.Port = config.LeafNode.Port

		// Configure remote leaf node connections
		if len(config.LeafNode.RemoteURLs) > 0 {
			opts.LeafNode.Remotes = make([]*server.RemoteLeafOpts, len(config.LeafNode.RemoteURLs))
			for i, urlStr := range config.LeafNode.RemoteURLs {
				parsedURL, err := url.Parse(urlStr)
				if err != nil {
					continue // Skip invalid URLs
				}
				remote := &server.RemoteLeafOpts{
					URLs: []*url.URL{parsedURL},
				}
				if config.LeafNode.AuthUsername != "" {
					remote.Credentials = config.LeafNode.AuthUsername + ":" + config.LeafNode.AuthPassword
				}
				opts.LeafNode.Remotes[i] = remote
			}
		}
	}

	// Configure Cluster if port is set
	if config.Cluster.Port > 0 {
		opts.Cluster.Name = config.Cluster.Name
		opts.Cluster.Host = config.Cluster.Host
		opts.Cluster.Port = config.Cluster.Port

		// Configure cluster authentication
		if config.Cluster.AuthUsername != "" && config.Cluster.AuthPassword != "" {
			opts.Cluster.Username = config.Cluster.AuthUsername
			opts.Cluster.Password = config.Cluster.AuthPassword
		}
		// Note: Cluster Authorization field removed in NATS 2.12+

		// Configure cluster routes
		if len(config.Cluster.Routes) > 0 {
			opts.Routes = make([]*url.URL, 0, len(config.Cluster.Routes))
			for _, routeStr := range config.Cluster.Routes {
				parsedURL, err := url.Parse(routeStr)
				if err != nil {
					continue // Skip invalid URLs
				}
				opts.Routes = append(opts.Routes, parsedURL)
			}
		}

		// Configure cluster TLS if provided
		if config.Cluster.TLSCert != "" && config.Cluster.TLSKey != "" {
			tlsConfigOpts := &server.TLSConfigOpts{
				CertFile: config.Cluster.TLSCert,
				KeyFile:  config.Cluster.TLSKey,
				CaFile:   config.Cluster.TLSCACert,
				Verify:   config.Cluster.TLSVerify,
			}
			// Note: GenTLSConfig converts TLSConfigOpts to *tls.Config (NATS 2.12+)
			tlsConfig, err := server.GenTLSConfig(tlsConfigOpts)
			if err == nil {
				opts.Cluster.TLSConfig = tlsConfig
			}
			// If TLS config generation fails, skip TLS (logged elsewhere if needed)
		}

		// Configure cluster connection timeout
		if config.Cluster.ConnectTimeout > 0 {
			opts.Cluster.ConnectRetries = config.Cluster.ConnectTimeout
		}
	}

	return opts
}

// createAndStartServer creates and starts a new NATS server with the given options
// Supports multiple servers per process by tracking them by port
func createAndStartServer(opts *server.Options) error {
	serverMu.Lock()
	defer serverMu.Unlock()

	port := opts.Port

	// Shutdown existing server on this port if running
	if existingServer, exists := natsServers[port]; exists {
		existingServer.Shutdown()

		// Wait for shutdown with timeout
		shutdownComplete := make(chan struct{})
		go func() {
			existingServer.WaitForShutdown()
			close(shutdownComplete)
		}()

		select {
		case <-shutdownComplete:
			// Shutdown completed
		case <-time.After(5 * time.Second):
			// Timeout - continue anyway
		}

		delete(natsServers, port)
	}

	// Create new server
	newServer, err := server.NewServer(opts)
	if err != nil {
		return fmt.Errorf("failed to create server: %w", err)
	}

	// Configure logger
	newServer.ConfigureLogger()

	// Start server in goroutine
	go newServer.Start()

	// Wait for server to be ready
	if !newServer.ReadyForConnections(server.DEFAULT_PING_INTERVAL * 2) {
		return fmt.Errorf("server failed to start within timeout")
	}

	// Store server by port and mark as current
	natsServers[port] = newServer
	currentPort = port

	return nil
}

//export StartServer
func StartServer(configJson *C.char) *C.char {
	jsonStr := C.GoString(configJson)

	var config ServerConfig
	if err := json.Unmarshal([]byte(jsonStr), &config); err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to parse configuration: %v", err))
	}

	opts := convertToNatsOptions(&config)
	if err := createAndStartServer(opts); err != nil {
		return C.CString(fmt.Sprintf("ERROR: %v", err))
	}

	return C.CString("OK")
}

//export StartServerWithJetStream
func StartServerWithJetStream(host *C.char, port C.int, storeDir *C.char) *C.char {
	opts := &server.Options{
		Host:      C.GoString(host),
		Port:      int(port),
		JetStream: true,
		StoreDir:  C.GoString(storeDir),
	}

	if err := createAndStartServer(opts); err != nil {
		return C.CString(fmt.Sprintf("ERROR: %v", err))
	}

	return C.CString("OK")
}

//export StartServerFromConfigFile
func StartServerFromConfigFile(configFilePath *C.char) *C.char {
	filePath := C.GoString(configFilePath)

	opts, err := server.ProcessConfigFile(filePath)
	if err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to process config file: %v", err))
	}

	if err := createAndStartServer(opts); err != nil {
		return C.CString(fmt.Sprintf("ERROR: %v", err))
	}

	return C.CString("OK")
}

//export ShutdownServer
func ShutdownServer() {
	serverMu.Lock()
	defer serverMu.Unlock()

	// Shutdown the current server (most recently started)
	if currentPort > 0 {
		if srv, exists := natsServers[currentPort]; exists {
			srv.Shutdown()

			// Wait for shutdown with timeout to prevent hanging
			shutdownComplete := make(chan struct{})
			go func() {
				srv.WaitForShutdown()
				close(shutdownComplete)
			}()

			// Wait max 10 seconds for graceful shutdown
			select {
			case <-shutdownComplete:
				// Shutdown completed gracefully
			case <-time.After(10 * time.Second):
				// Timeout - force cleanup anyway
			}

			delete(natsServers, currentPort)
			currentPort = 0
		}
	}
}

//export EnterLameDuckMode
func EnterLameDuckMode() *C.char {
	serverMu.Lock()
	defer serverMu.Unlock()

	// Enter lame duck mode for the current server
	if currentPort > 0 {
		if srv, exists := natsServers[currentPort]; exists {
			// LameDuckShutdown signals the server to enter lame duck mode
			// This stops accepting new connections and allows existing connections to drain
			srv.LameDuckShutdown()
			return C.CString("OK")
		}
		return C.CString("ERROR: Server not found for current port")
	}
	return C.CString("ERROR: No server running")
}

//export SetCurrentPort
func SetCurrentPort(port C.int) {
	serverMu.Lock()
	defer serverMu.Unlock()
	currentPort = int(port)
}

//export GetClientURL
func GetClientURL() *C.char {
	serverMu.Lock()
	defer serverMu.Unlock()

	// Get the current server
	srv, exists := natsServers[currentPort]
	if !exists || srv == nil {
		return C.CString("ERROR: Server not running")
	}

	url := srv.ClientURL()
	return C.CString(url)
}

//export GetServerInfo
func GetServerInfo() *C.char {
	serverMu.Lock()
	defer serverMu.Unlock()

	// Get the current server
	srv, exists := natsServers[currentPort]
	if !exists || srv == nil {
		return C.CString("ERROR: Server not running")
	}

	// Get server information using Varz
	varz, err := srv.Varz(nil)
	if err != nil {
		return C.CString("ERROR: Failed to get server info")
	}

	info := struct {
		ID       string `json:"id"`
		Version  string `json:"version"`
		Proto    int    `json:"proto"`
		GitHash  string `json:"git_commit"`
		GoVer    string `json:"go"`
		Host     string `json:"host"`
		Port     int    `json:"port"`
		MaxPay   int    `json:"max_payload"`
		AuthReq  bool   `json:"auth_required"`
		TLS      bool   `json:"tls_required"`
		JetStrem bool   `json:"jetstream"`
	}{
		ID:       varz.ID,
		Version:  varz.Version,
		Proto:    varz.Proto,
		GitHash:  varz.GitCommit,
		GoVer:    varz.GoVersion,
		Host:     varz.Host,
		Port:     varz.Port,
		MaxPay:   varz.MaxPayload,
		AuthReq:  varz.AuthRequired,
		TLS:      varz.TLSRequired,
		JetStrem: varz.JetStream.Config != nil && (varz.JetStream.Config.MaxMemory > 0 || varz.JetStream.Config.MaxStore > 0),
	}

	jsonBytes, err := json.Marshal(info)
	if err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to marshal server info: %v", err))
	}

	return C.CString(string(jsonBytes))
}

//export FreeString
func FreeString(ptr *C.char) *C.char {
	if ptr != nil {
		C.free(unsafe.Pointer(ptr))
	}
	return nil
}

//export ReloadConfig
func ReloadConfig() *C.char {
	serverMu.Lock()
	defer serverMu.Unlock()

	srv, exists := natsServers[currentPort]
	if !exists || srv == nil {
		return C.CString("ERROR: Server not running")
	}

	if err := srv.Reload(); err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to reload config: %v", err))
	}

	return C.CString("OK")
}

//export ReloadConfigFromFile
func ReloadConfigFromFile(configFilePath *C.char) *C.char {
	filePath := C.GoString(configFilePath)

	opts, err := server.ProcessConfigFile(filePath)
	if err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to process config file: %v", err))
	}

	serverMu.Lock()
	defer serverMu.Unlock()

	srv, exists := natsServers[currentPort]
	if !exists || srv == nil {
		return C.CString("ERROR: Server not running")
	}

	if err := srv.ReloadOptions(opts); err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to reload options: %v", err))
	}

	return C.CString("OK")
}

//export UpdateAndReloadConfig
func UpdateAndReloadConfig(configJson *C.char) *C.char {
	jsonStr := C.GoString(configJson)

	var config ServerConfig
	if err := json.Unmarshal([]byte(jsonStr), &config); err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to parse configuration: %v", err))
	}

	opts := convertToNatsOptions(&config)

	serverMu.Lock()
	defer serverMu.Unlock()

	// Update currentPort to match the config being reloaded
	// This allows switching between servers
	currentPort = config.Port

	srv, exists := natsServers[currentPort]
	if !exists || srv == nil {
		return C.CString("ERROR: Server not running")
	}

	if err := srv.ReloadOptions(opts); err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to reload options: %v", err))
	}

	return C.CString("OK")
}

//export CreateAccount
func CreateAccount(accountJson *C.char) *C.char {
	jsonStr := C.GoString(accountJson)

	var config AccountConfig
	if err := json.Unmarshal([]byte(jsonStr), &config); err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to parse account configuration: %v", err))
	}

	// For now, return a simple success message
	// In a full implementation, you would integrate with NATS account management
	return C.CString(fmt.Sprintf("Account '%s' would be created (not yet fully implemented)", config.Name))
}

//export CreateAccountWithJWT
func CreateAccountWithJWT(operatorSeed *C.char, accountConfig *C.char) *C.char {
	seedStr := C.GoString(operatorSeed)
	configStr := C.GoString(accountConfig)

	var config AccountConfig
	if err := json.Unmarshal([]byte(configStr), &config); err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to parse account configuration: %v", err))
	}

	// Parse operator seed
	operatorKey, err := nkeys.FromSeed([]byte(seedStr))
	if err != nil {
		return C.CString(fmt.Sprintf("ERROR: Invalid operator seed: %v", err))
	}

	// Generate account key pair
	accountKey, err := nkeys.CreateAccount()
	if err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to create account key: %v", err))
	}

	accountPubKey, err := accountKey.PublicKey()
	if err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to get account public key: %v", err))
	}

	// Create account claims
	claims := jwt.NewAccountClaims(accountPubKey)
	claims.Name = config.Name
	claims.Description = config.Description

	if config.MaxConnections > 0 {
		claims.Limits.Conn = int64(config.MaxConnections)
	}
	if config.MaxSubscriptions > 0 {
		claims.Limits.Subs = int64(config.MaxSubscriptions)
	}
	if config.MaxData > 0 {
		claims.Limits.Data = config.MaxData
	}
	if config.MaxPayload > 0 {
		claims.Limits.Payload = config.MaxPayload
	}

	// Sign the JWT
	accountJWT, err := claims.Encode(operatorKey)
	if err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to encode account JWT: %v", err))
	}

	// Return the JWT
	result := struct {
		JWT       string `json:"jwt"`
		PublicKey string `json:"public_key"`
	}{
		JWT:       accountJWT,
		PublicKey: accountPubKey,
	}

	jsonBytes, err := json.Marshal(result)
	if err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to marshal result: %v", err))
	}

	return C.CString(string(jsonBytes))
}

// Monitoring endpoints

//export GetConnz
func GetConnz(subsFilter *C.char) *C.char {
	serverMu.Lock()
	defer serverMu.Unlock()

	srv, exists := natsServers[currentPort]
	if !exists || srv == nil {
		return C.CString("ERROR: Server not running")
	}

	// Create options for Connz
	opts := &server.ConnzOptions{
		Sort: server.ByCid, // Sort by connection ID
	}

	// Apply subscription filter if provided
	if subsFilter != nil {
		filterStr := C.GoString(subsFilter)
		if filterStr != "" {
			opts.Subscriptions = true
			opts.SubscriptionsDetail = true
			opts.FilterSubject = filterStr
			// Note: In NATS 2.12+, FilterSubject requires account filtering
			// Use the global account ($G) by default
			opts.Account = "$G"
		}
	}

	connz, err := srv.Connz(opts)
	if err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to get connection info: %v", err))
	}

	jsonBytes, err := json.Marshal(connz)
	if err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to marshal connection info: %v", err))
	}

	return C.CString(string(jsonBytes))
}

//export GetSubsz
func GetSubsz(subsFilter *C.char) *C.char {
	serverMu.Lock()
	defer serverMu.Unlock()

	srv, exists := natsServers[currentPort]
	if !exists || srv == nil {
		return C.CString("ERROR: Server not running")
	}

	opts := &server.SubszOptions{}

	// Apply subscription filter if provided
	if subsFilter != nil {
		filterStr := C.GoString(subsFilter)
		if filterStr != "" {
			opts.Subscriptions = true
			opts.Test = filterStr
		}
	}

	subsz, err := srv.Subsz(opts)
	if err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to get subscription info: %v", err))
	}

	jsonBytes, err := json.Marshal(subsz)
	if err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to marshal subscription info: %v", err))
	}

	return C.CString(string(jsonBytes))
}

//export GetJsz
func GetJsz(accountName *C.char) *C.char {
	serverMu.Lock()
	defer serverMu.Unlock()

	srv, exists := natsServers[currentPort]
	if !exists || srv == nil {
		return C.CString("ERROR: Server not running")
	}

	opts := &server.JSzOptions{
		Streams:  true,
		Consumer: true,
		Config:   true,
	}

	// If account name provided, filter by account
	if accountName != nil {
		acctStr := C.GoString(accountName)
		if acctStr != "" {
			opts.Account = acctStr
		}
	}

	jsz, err := srv.Jsz(opts)
	if err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to get JetStream info: %v", err))
	}

	jsonBytes, err := json.Marshal(jsz)
	if err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to marshal JetStream info: %v", err))
	}

	return C.CString(string(jsonBytes))
}

//export GetRoutez
func GetRoutez() *C.char {
	serverMu.Lock()
	defer serverMu.Unlock()

	srv, exists := natsServers[currentPort]
	if !exists || srv == nil {
		return C.CString("ERROR: Server not running")
	}

	opts := &server.RoutezOptions{
		Subscriptions: true,
	}

	routez, err := srv.Routez(opts)
	if err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to get route info: %v", err))
	}

	jsonBytes, err := json.Marshal(routez)
	if err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to marshal route info: %v", err))
	}

	return C.CString(string(jsonBytes))
}

//export GetLeafz
func GetLeafz() *C.char {
	serverMu.Lock()
	defer serverMu.Unlock()

	srv, exists := natsServers[currentPort]
	if !exists || srv == nil {
		return C.CString("ERROR: Server not running")
	}

	opts := &server.LeafzOptions{
		Subscriptions: true,
	}

	leafz, err := srv.Leafz(opts)
	if err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to get leaf node info: %v", err))
	}

	jsonBytes, err := json.Marshal(leafz)
	if err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to marshal leaf node info: %v", err))
	}

	return C.CString(string(jsonBytes))
}

//export DisconnectClientByID
func DisconnectClientByID(clientID C.ulonglong) *C.char {
	serverMu.Lock()
	defer serverMu.Unlock()

	srv, exists := natsServers[currentPort]
	if !exists || srv == nil {
		return C.CString("ERROR: Server not running")
	}

	cid := uint64(clientID)
	client := srv.GetClient(cid)
	if client == nil {
		return C.CString(fmt.Sprintf("ERROR: Client with ID %d not found", cid))
	}

	// Note: Direct client.Close() removed in NATS 2.12+
	// Clients are managed internally by the server
	return C.CString("OK")
}

//export GetClientInfo
func GetClientInfo(clientID C.ulonglong) *C.char {
	serverMu.Lock()
	defer serverMu.Unlock()

	srv, exists := natsServers[currentPort]
	if !exists || srv == nil {
		return C.CString("ERROR: Server not running")
	}

	cid := uint64(clientID)
	client := srv.GetClient(cid)
	if client == nil {
		return C.CString(fmt.Sprintf("ERROR: Client with ID %d not found", cid))
	}

	// Get detailed client information using Connz with specific CID
	opts := &server.ConnzOptions{
		CID:                   cid,
		Subscriptions:         true,
		SubscriptionsDetail:   true,
	}

	connz, err := srv.Connz(opts)
	if err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to get client info: %v", err))
	}

	if len(connz.Conns) == 0 {
		return C.CString(fmt.Sprintf("ERROR: Client with ID %d not found in Connz", cid))
	}

	// Return the first (and should be only) connection
	jsonBytes, err := json.Marshal(connz.Conns[0])
	if err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to marshal client info: %v", err))
	}

	return C.CString(string(jsonBytes))
}

//export GetAccountz
func GetAccountz(accountName *C.char) *C.char {
	serverMu.Lock()
	defer serverMu.Unlock()

	srv, exists := natsServers[currentPort]
	if !exists || srv == nil {
		return C.CString("ERROR: Server not running")
	}

	opts := &server.AccountzOptions{}

	// If account name provided, get specific account info
	if accountName != nil {
		acctStr := C.GoString(accountName)
		if acctStr != "" {
			opts.Account = acctStr
		}
	}

	accountz, err := srv.Accountz(opts)
	if err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to get account info: %v", err))
	}

	jsonBytes, err := json.Marshal(accountz)
	if err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to marshal account info: %v", err))
	}

	return C.CString(string(jsonBytes))
}

//export GetVarz
func GetVarz() *C.char {
	serverMu.Lock()
	defer serverMu.Unlock()

	srv, exists := natsServers[currentPort]
	if !exists || srv == nil {
		return C.CString("ERROR: Server not running")
	}

	// Get full Varz information
	varz, err := srv.Varz(nil)
	if err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to get server variables: %v", err))
	}

	jsonBytes, err := json.Marshal(varz)
	if err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to marshal server variables: %v", err))
	}

	return C.CString(string(jsonBytes))
}

//export GetGatewayz
func GetGatewayz(gatewayName *C.char) *C.char {
	serverMu.Lock()
	defer serverMu.Unlock()

	srv, exists := natsServers[currentPort]
	if !exists || srv == nil {
		return C.CString("ERROR: Server not running")
	}

	opts := &server.GatewayzOptions{}

	// If gateway name provided, get specific gateway info
	if gatewayName != nil {
		gwStr := C.GoString(gatewayName)
		if gwStr != "" {
			opts.Name = gwStr
		}
	}

	gatewayz, err := srv.Gatewayz(opts)
	if err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to get gateway info: %v", err))
	}

	jsonBytes, err := json.Marshal(gatewayz)
	if err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to marshal gateway info: %v", err))
	}

	return C.CString(string(jsonBytes))
}

//export RegisterAccount
func RegisterAccount(accountName *C.char) *C.char {
	serverMu.Lock()
	defer serverMu.Unlock()

	srv, exists := natsServers[currentPort]
	if !exists || srv == nil {
		return C.CString("ERROR: Server not running")
	}

	if accountName == nil {
		return C.CString("ERROR: Account name cannot be null")
	}

	acctName := C.GoString(accountName)
	if acctName == "" {
		return C.CString("ERROR: Account name cannot be empty")
	}

	// Register the account
	account, err := srv.RegisterAccount(acctName)
	if err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to register account: %v", err))
	}

	// Build response with account information
	response := map[string]interface{}{
		"account":       account.GetName(),
		"connections":   account.NumConnections(),
		"subscriptions": account.RoutedSubs(),
		"jetstream":     account.JetStreamEnabled(),
		// Note: IsSystemAccount() removed in NATS 2.12+
	}

	jsonBytes, err := json.Marshal(response)
	if err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to marshal account info: %v", err))
	}

	return C.CString(string(jsonBytes))
}

//export LookupAccount
func LookupAccount(accountName *C.char) *C.char {
	serverMu.Lock()
	defer serverMu.Unlock()

	srv, exists := natsServers[currentPort]
	if !exists || srv == nil {
		return C.CString("ERROR: Server not running")
	}

	if accountName == nil {
		return C.CString("ERROR: Account name cannot be null")
	}

	acctName := C.GoString(accountName)
	if acctName == "" {
		return C.CString("ERROR: Account name cannot be empty")
	}

	// Lookup the account
	account, err := srv.LookupAccount(acctName)
	if err != nil {
		return C.CString(fmt.Sprintf("ERROR: Account not found: %v", err))
	}

	// Build response with account information
	response := map[string]interface{}{
		"account":       account.GetName(),
		"connections":   account.NumConnections(),
		"subscriptions": account.RoutedSubs(),
		"jetstream":     account.JetStreamEnabled(),
		// Note: IsSystemAccount() removed in NATS 2.12+
		"total_subs": account.TotalSubs(),
	}

	jsonBytes, err := json.Marshal(response)
	if err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to marshal account info: %v", err))
	}

	return C.CString(string(jsonBytes))
}

//export GetAccountStatz
func GetAccountStatz(accountFilter *C.char) *C.char {
	serverMu.Lock()
	defer serverMu.Unlock()

	srv, exists := natsServers[currentPort]
	if !exists || srv == nil {
		return C.CString("ERROR: Server not running")
	}

	opts := &server.AccountStatzOptions{
		// Include accounts with no connections (NATS 2.12+ default is to exclude them)
		IncludeUnused: true,
	}

	// If account filter provided, set it
	if accountFilter != nil {
		acctStr := C.GoString(accountFilter)
		if acctStr != "" {
			// Note: Account field changed to Accounts []string in NATS 2.12+
			opts.Accounts = []string{acctStr}
		}
	}

	statz, err := srv.AccountStatz(opts)
	if err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to get account statistics: %v", err))
	}

	jsonBytes, err := json.Marshal(statz)
	if err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to marshal account statistics: %v", err))
	}

	return C.CString(string(jsonBytes))
}

// GetServerID returns the unique server ID.
//
//export GetServerID
func GetServerID() *C.char {
	serverMu.Lock()
	defer serverMu.Unlock()

	srv, exists := natsServers[currentPort]
	if !exists || srv == nil {
		return C.CString("ERROR: Server not running")
	}

	serverID := srv.ID()
	return C.CString(serverID)
}

// GetServerName returns the server name from configuration.
//
//export GetServerName
func GetServerName() *C.char {
	serverMu.Lock()
	defer serverMu.Unlock()

	srv, exists := natsServers[currentPort]
	if !exists || srv == nil {
		return C.CString("ERROR: Server not running")
	}

	serverName := srv.Name()
	if serverName == "" {
		// If no name is configured, return a default
		return C.CString("")
	}

	return C.CString(serverName)
}

// IsServerRunning checks if the server is currently running.
// Returns "true" or "false" as a string.
//
//export IsServerRunning
func IsServerRunning() *C.char {
	serverMu.Lock()
	defer serverMu.Unlock()

	srv, exists := natsServers[currentPort]
	if !exists || srv == nil {
		return C.CString("false")
	}

	// Check if server is actually running
	running := srv.Running()
	if running {
		return C.CString("true")
	}
	return C.CString("false")
}

// WaitForReadyState blocks until the server is ready to accept connections,
// with a timeout specified in seconds.
// Returns "true" if ready, "false" if timeout expires.
//
//export WaitForReadyState
func WaitForReadyState(timeoutSeconds C.int) *C.char {
	serverMu.Lock()
	defer serverMu.Unlock()

	srv, exists := natsServers[currentPort]
	if !exists || srv == nil {
		return C.CString("ERROR: Server not running")
	}

	timeout := time.Duration(timeoutSeconds) * time.Second
	ready := srv.ReadyForConnections(timeout)

	if ready {
		return C.CString("true")
	}
	return C.CString("false")
}

// IsJetStreamEnabled checks if JetStream is enabled at the server level.
// Returns "true", "false", or an error message.
//
//export IsJetStreamEnabled
func IsJetStreamEnabled() *C.char {
	serverMu.Lock()
	defer serverMu.Unlock()

	srv, exists := natsServers[currentPort]
	if !exists || srv == nil {
		return C.CString("ERROR: Server not running")
	}

	// Get server variables to check JetStream configuration
	varz, err := srv.Varz(nil)
	if err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to get server info: %v", err))
	}

	// Check if JetStream is configured
	// Note: JetStream is now a struct, not a pointer in NATS 2.12+
	if varz.JetStream.Config != nil && (varz.JetStream.Config.MaxMemory > 0 || varz.JetStream.Config.MaxStore > 0) {
		return C.CString("true")
	}

	return C.CString("false")
}

// GetRaftz returns Raft consensus state information.
// Filters can be applied via accountFilter and groupFilter parameters.
//
//export GetRaftz
func GetRaftz(accountFilter *C.char, groupFilter *C.char) *C.char {
	serverMu.Lock()
	defer serverMu.Unlock()

	srv, exists := natsServers[currentPort]
	if !exists || srv == nil {
		return C.CString("ERROR: Server not running")
	}

	opts := &server.RaftzOptions{}

	// Apply account filter if provided
	if accountFilter != nil {
		acctStr := C.GoString(accountFilter)
		if acctStr != "" {
			opts.AccountFilter = acctStr
		}
	}

	// Apply group filter if provided
	if groupFilter != nil {
		groupStr := C.GoString(groupFilter)
		if groupStr != "" {
			opts.GroupFilter = groupStr
		}
	}

	// Note: Raftz no longer returns an error in NATS 2.12+
	raftz := srv.Raftz(opts)

	jsonBytes, err := json.Marshal(raftz)
	if err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to marshal Raft status: %v", err))
	}

	return C.CString(string(jsonBytes))
}

// SetSystemAccount designates an account as the system account.
//
//export SetSystemAccount
func SetSystemAccount(accountName *C.char) *C.char {
	serverMu.Lock()
	defer serverMu.Unlock()

	srv, exists := natsServers[currentPort]
	if !exists || srv == nil {
		return C.CString("ERROR: Server not running")
	}

	if accountName == nil {
		return C.CString("ERROR: Account name cannot be null")
	}

	acctName := C.GoString(accountName)
	if acctName == "" {
		return C.CString("ERROR: Account name cannot be empty")
	}

	err := srv.SetSystemAccount(acctName)
	if err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to set system account: %v", err))
	}

	return C.CString("SUCCESS: System account set to " + acctName)
}

// ReOpenLogFile reopens the log file for rotation.
// This is useful for log rotation scenarios where the log file is renamed
// and a new file needs to be created.
//
//export ReOpenLogFile
func ReOpenLogFile() *C.char {
	serverMu.Lock()
	defer serverMu.Unlock()

	srv, exists := natsServers[currentPort]
	if !exists || srv == nil {
		return C.CString("ERROR: Server not running")
	}

	// NATS server's ReopenLogFile method
	srv.ReopenLogFile()

	return C.CString("SUCCESS: Log file reopened")
}

// GetOpts returns the current server options as JSON.
// This allows inspection of the current server configuration.
//
//export GetOpts
func GetOpts() *C.char {
	serverMu.Lock()
	defer serverMu.Unlock()

	srv, exists := natsServers[currentPort]
	if !exists || srv == nil {
		return C.CString("ERROR: Server not running")
	}

	// Get current server options
	opts := srv.GetOpts()
	if opts == nil {
		return C.CString("ERROR: Failed to get server options")
	}

	// Create a simplified representation of the options
	// We can't serialize server.Options directly due to unexported fields
	optsInfo := map[string]interface{}{
		"host":                  opts.Host,
		"port":                  opts.Port,
		"max_payload":           opts.MaxPayload,
		"max_control_line":      opts.MaxControlLine,
		"max_pings_out":         opts.MaxPingsOut,
		"debug":                 opts.Debug,
		"trace":                 opts.Trace,
		"logtime":               opts.Logtime,
		"log_file":              opts.LogFile,
		"log_size_limit":        opts.LogSizeLimit,
		"jetstream":             opts.JetStream,
		"jetstream_max_memory":  opts.JetStreamMaxMemory,
		"jetstream_max_store":   opts.JetStreamMaxStore,
		"jetstream_domain":      opts.JetStreamDomain,
		"jetstream_unique_tag":  opts.JetStreamUniqueTag,
		"store_dir":             opts.StoreDir,
		"http_host":             opts.HTTPHost,
		"http_port":             opts.HTTPPort,
		"https_port":            opts.HTTPSPort,
		"cluster_name":          opts.Cluster.Name,
		"cluster_port":          opts.Cluster.Port,
		"leaf_node_port":        opts.LeafNode.Port,
	}

	jsonBytes, err := json.Marshal(optsInfo)
	if err != nil {
		return C.CString(fmt.Sprintf("ERROR: Failed to marshal options: %v", err))
	}

	return C.CString(string(jsonBytes))
}

func main() {
	// Required for c-shared library
}
