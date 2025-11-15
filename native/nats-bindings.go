package main

/*
#include <stdlib.h>
*/
import "C"
import (
	"encoding/json"
	"fmt"
	"os"
	"sync"
	"unsafe"

	"github.com/nats-io/jwt/v2"
	"github.com/nats-io/nats-server/v2/server"
	"github.com/nats-io/nkeys"
)

var (
	// Global server instance
	natsServer *server.Server
	serverMu   sync.Mutex
)

// ServerConfig represents the configuration for the NATS server
type ServerConfig struct {
	Host                string         `json:"host"`
	Port                int            `json:"port"`
	MaxPayload          int            `json:"max_payload"`
	MaxControlLine      int            `json:"max_control_line"`
	PingInterval        int            `json:"ping_interval"`
	MaxPingsOut         int            `json:"max_pings_out"`
	WriteDeadline       int            `json:"write_deadline"`
	Debug               bool           `json:"debug"`
	Trace               bool           `json:"trace"`
	Jetstream           bool           `json:"jetstream"`
	JetstreamStoreDir   string         `json:"jetstream_store_dir"`
	JetstreamMaxMemory  int64          `json:"jetstream_max_memory"`
	JetstreamMaxStore   int64          `json:"jetstream_max_store"`
	HTTPPort            int            `json:"http_port"`
	HTTPHost            string         `json:"http_host"`
	HTTPSPort           int            `json:"https_port"`
	Auth                AuthConfig     `json:"auth"`
	LeafNode            LeafNodeConfig `json:"leaf_node"`
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
			for i, url := range config.LeafNode.RemoteURLs {
				remote := &server.RemoteLeafOpts{
					URLs: []*server.URL{{Value: url}},
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
func createAndStartServer(opts *server.Options) error {
	serverMu.Lock()
	defer serverMu.Unlock()

	// Shutdown existing server if running
	if natsServer != nil {
		natsServer.Shutdown()
		natsServer.WaitForShutdown()
		natsServer = nil
	}

	// Create new server
	var err error
	natsServer, err = server.NewServer(opts)
	if err != nil {
		return fmt.Errorf("failed to create server: %w", err)
	}

	// Configure logger
	natsServer.ConfigureLogger()

	// Start server in goroutine
	go natsServer.Start()

	// Wait for server to be ready
	if !natsServer.ReadyForConnections(server.DEFAULT_PING_INTERVAL * 2) {
		return fmt.Errorf("server failed to start within timeout")
	}

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

	if natsServer != nil {
		natsServer.Shutdown()
		natsServer.WaitForShutdown()
		natsServer = nil
	}
}

//export GetClientURL
func GetClientURL() *C.char {
	serverMu.Lock()
	defer serverMu.Unlock()

	if natsServer == nil {
		return C.CString("ERROR: Server not running")
	}

	url := natsServer.ClientURL()
	return C.CString(url)
}

//export GetServerInfo
func GetServerInfo() *C.char {
	serverMu.Lock()
	defer serverMu.Unlock()

	if natsServer == nil {
		return C.CString("ERROR: Server not running")
	}

	info := struct {
		ID       string `json:"id"`
		Version  string `json:"version"`
		Proto    int    `json:"proto"`
		GitHash  string `json:"git_commit"`
		GoVer    string `json:"go"`
		Host     string `json:"host"`
		Port     int    `json:"port"`
		MaxPay   int32  `json:"max_payload"`
		AuthReq  bool   `json:"auth_required"`
		TLS      bool   `json:"tls_required"`
		JetStrem bool   `json:"jetstream"`
	}{
		ID:       natsServer.ID(),
		Version:  natsServer.Info().Version,
		Proto:    natsServer.Info().Proto,
		GitHash:  natsServer.Info().GitCommit,
		GoVer:    natsServer.Info().GoVersion,
		Host:     natsServer.Info().Host,
		Port:     natsServer.Info().Port,
		MaxPay:   natsServer.Info().MaxPayload,
		AuthReq:  natsServer.Info().AuthRequired,
		TLS:      natsServer.Info().TLSRequired,
		JetStrem: natsServer.Info().JetStream,
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

	if natsServer == nil {
		return C.CString("ERROR: Server not running")
	}

	if err := natsServer.Reload(); err != nil {
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

	if natsServer == nil {
		return C.CString("ERROR: Server not running")
	}

	if err := natsServer.ReloadOptions(opts); err != nil {
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

	if natsServer == nil {
		return C.CString("ERROR: Server not running")
	}

	if err := natsServer.ReloadOptions(opts); err != nil {
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
