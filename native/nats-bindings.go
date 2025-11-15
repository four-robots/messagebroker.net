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
	Jetstream          bool           `json:"jetstream"`
	JetstreamStoreDir  string         `json:"jetstream_store_dir"`
	JetstreamMaxMemory int64          `json:"jetstream_max_memory"`
	JetstreamMaxStore  int64          `json:"jetstream_max_store"`
	HTTPPort           int            `json:"http_port"`
	HTTPHost           string         `json:"http_host"`
	HTTPSPort          int            `json:"https_port"`
	Auth               AuthConfig     `json:"auth"`
	LeafNode           LeafNodeConfig `json:"leaf_node"`
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
		HTTPHost:       config.HTTPHost,
		HTTPPort:       config.HTTPPort,
		HTTPSPort:      config.HTTPSPort,
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
			// LameDuckMode signals the server to enter lame duck mode
			// This stops accepting new connections and allows existing connections to drain
			srv.LameDuckMode()
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

func main() {
	// Required for c-shared library
}
