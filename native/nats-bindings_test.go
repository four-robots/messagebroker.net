package main

/*
#include <stdlib.h>
*/
import "C"

import (
	"encoding/json"
	"strings"
	"testing"
	"time"
	"unsafe"

	"github.com/nats-io/nats-server/v2/server"
)

// Helper function to start a test server
func startTestServer(t *testing.T, port int) *server.Server {
	opts := &server.Options{
		Host: "127.0.0.1",
		Port: port,
	}

	srv, err := server.NewServer(opts)
	if err != nil {
		t.Fatalf("Failed to create server: %v", err)
	}

	go srv.Start()

	// Wait for server to be ready
	if !srv.ReadyForConnections(5 * time.Second) {
		t.Fatal("Server not ready for connections")
	}

	// Store in global map
	serverMu.Lock()
	natsServers[port] = srv
	currentPort = port
	serverMu.Unlock()

	return srv
}

// Helper function to stop test server
func stopTestServer(t *testing.T, srv *server.Server, port int) {
	serverMu.Lock()
	delete(natsServers, port)
	serverMu.Unlock()

	srv.Shutdown()
	srv.WaitForShutdown()
}

// Helper to check if response is an error
func isErrorResponse(response string) bool {
	return strings.HasPrefix(response, "ERROR:")
}

// Test GetConnz with server running
func TestGetConnz_ServerRunning(t *testing.T) {
	port := 14222
	srv := startTestServer(t, port)
	defer stopTestServer(t, srv, port)

	result := GetConnz(nil)
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if isErrorResponse(response) {
		t.Fatalf("Expected success, got error: %s", response)
	}

	// Validate JSON structure
	var connz map[string]interface{}
	if err := json.Unmarshal([]byte(response), &connz); err != nil {
		t.Fatalf("Failed to parse Connz JSON: %v", err)
	}

	// Check for expected fields
	if _, exists := connz["num_connections"]; !exists {
		t.Error("Expected 'num_connections' field in Connz response")
	}
}

// Test GetConnz without server
func TestGetConnz_ServerNotRunning(t *testing.T) {
	// Clear server state
	serverMu.Lock()
	currentPort = 99999 // Non-existent port
	serverMu.Unlock()

	result := GetConnz(nil)
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if !isErrorResponse(response) {
		t.Fatal("Expected error when server not running")
	}

	if !strings.Contains(response, "Server not running") {
		t.Errorf("Expected 'Server not running' error, got: %s", response)
	}
}

// Test GetConnz with subscription filter
func TestGetConnz_WithSubscriptionFilter(t *testing.T) {
	port := 14223
	srv := startTestServer(t, port)
	defer stopTestServer(t, srv, port)

	filter := C.CString("test.*")
	defer C.free(unsafe.Pointer(filter))

	result := GetConnz(filter)
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if isErrorResponse(response) {
		t.Fatalf("Expected success, got error: %s", response)
	}

	// Validate JSON
	var connz map[string]interface{}
	if err := json.Unmarshal([]byte(response), &connz); err != nil {
		t.Fatalf("Failed to parse Connz JSON: %v", err)
	}
}

// Test GetSubsz with server running
func TestGetSubsz_ServerRunning(t *testing.T) {
	port := 14224
	srv := startTestServer(t, port)
	defer stopTestServer(t, srv, port)

	result := GetSubsz(nil)
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if isErrorResponse(response) {
		t.Fatalf("Expected success, got error: %s", response)
	}

	var subsz map[string]interface{}
	if err := json.Unmarshal([]byte(response), &subsz); err != nil {
		t.Fatalf("Failed to parse Subsz JSON: %v", err)
	}

	if _, exists := subsz["num_subscriptions"]; !exists {
		t.Error("Expected 'num_subscriptions' field in Subsz response")
	}
}

// Test GetSubsz without server
func TestGetSubsz_ServerNotRunning(t *testing.T) {
	serverMu.Lock()
	currentPort = 99999
	serverMu.Unlock()

	result := GetSubsz(nil)
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if !isErrorResponse(response) {
		t.Fatal("Expected error when server not running")
	}
}

// Test GetJsz with JetStream enabled
func TestGetJsz_WithJetStream(t *testing.T) {
	port := 14225

	// Create server with JetStream
	opts := &server.Options{
		Host:      "127.0.0.1",
		Port:      port,
		JetStream: true,
		StoreDir:  t.TempDir(),
	}

	srv, err := server.NewServer(opts)
	if err != nil {
		t.Fatalf("Failed to create server: %v", err)
	}

	go srv.Start()
	if !srv.ReadyForConnections(5 * time.Second) {
		t.Fatal("Server not ready")
	}

	serverMu.Lock()
	natsServers[port] = srv
	currentPort = port
	serverMu.Unlock()

	defer stopTestServer(t, srv, port)

	result := GetJsz(nil)
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if isErrorResponse(response) {
		t.Fatalf("Expected success, got error: %s", response)
	}

	var jsz map[string]interface{}
	if err := json.Unmarshal([]byte(response), &jsz); err != nil {
		t.Fatalf("Failed to parse Jsz JSON: %v", err)
	}

	// Check for JetStream-specific fields
	if _, exists := jsz["config"]; !exists {
		t.Error("Expected 'config' field in Jsz response")
	}
}

// Test GetJsz without server
func TestGetJsz_ServerNotRunning(t *testing.T) {
	serverMu.Lock()
	currentPort = 99999
	serverMu.Unlock()

	result := GetJsz(nil)
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if !isErrorResponse(response) {
		t.Fatal("Expected error when server not running")
	}
}

// Test GetRoutez with server running
func TestGetRoutez_ServerRunning(t *testing.T) {
	port := 14226
	srv := startTestServer(t, port)
	defer stopTestServer(t, srv, port)

	result := GetRoutez()
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if isErrorResponse(response) {
		t.Fatalf("Expected success, got error: %s", response)
	}

	var routez map[string]interface{}
	if err := json.Unmarshal([]byte(response), &routez); err != nil {
		t.Fatalf("Failed to parse Routez JSON: %v", err)
	}

	if _, exists := routez["num_routes"]; !exists {
		t.Error("Expected 'num_routes' field in Routez response")
	}
}

// Test GetRoutez without server
func TestGetRoutez_ServerNotRunning(t *testing.T) {
	serverMu.Lock()
	currentPort = 99999
	serverMu.Unlock()

	result := GetRoutez()
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if !isErrorResponse(response) {
		t.Fatal("Expected error when server not running")
	}
}

// Test GetLeafz with server running
func TestGetLeafz_ServerRunning(t *testing.T) {
	port := 14227
	srv := startTestServer(t, port)
	defer stopTestServer(t, srv, port)

	result := GetLeafz()
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if isErrorResponse(response) {
		t.Fatalf("Expected success, got error: %s", response)
	}

	var leafz map[string]interface{}
	if err := json.Unmarshal([]byte(response), &leafz); err != nil {
		t.Fatalf("Failed to parse Leafz JSON: %v", err)
	}
}

// Test GetLeafz without server
func TestGetLeafz_ServerNotRunning(t *testing.T) {
	serverMu.Lock()
	currentPort = 99999
	serverMu.Unlock()

	result := GetLeafz()
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if !isErrorResponse(response) {
		t.Fatal("Expected error when server not running")
	}
}

// Test DisconnectClientByID with non-existent client
func TestDisconnectClientByID_ClientNotFound(t *testing.T) {
	port := 14228
	srv := startTestServer(t, port)
	defer stopTestServer(t, srv, port)

	result := DisconnectClientByID(C.ulonglong(99999))
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if !isErrorResponse(response) {
		t.Fatal("Expected error for non-existent client")
	}

	if !strings.Contains(response, "not found") {
		t.Errorf("Expected 'not found' error, got: %s", response)
	}
}

// Test DisconnectClientByID without server
func TestDisconnectClientByID_ServerNotRunning(t *testing.T) {
	serverMu.Lock()
	currentPort = 99999
	serverMu.Unlock()

	result := DisconnectClientByID(C.ulonglong(1))
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if !isErrorResponse(response) {
		t.Fatal("Expected error when server not running")
	}
}

// Test GetClientInfo with non-existent client
func TestGetClientInfo_ClientNotFound(t *testing.T) {
	port := 14229
	srv := startTestServer(t, port)
	defer stopTestServer(t, srv, port)

	result := GetClientInfo(C.ulonglong(99999))
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if !isErrorResponse(response) {
		t.Fatal("Expected error for non-existent client")
	}

	if !strings.Contains(response, "not found") {
		t.Errorf("Expected 'not found' error, got: %s", response)
	}
}

// Test GetClientInfo without server
func TestGetClientInfo_ServerNotRunning(t *testing.T) {
	serverMu.Lock()
	currentPort = 99999
	serverMu.Unlock()

	result := GetClientInfo(C.ulonglong(1))
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if !isErrorResponse(response) {
		t.Fatal("Expected error when server not running")
	}
}

// Test GetAccountz with server running
func TestGetAccountz_ServerRunning(t *testing.T) {
	port := 14230
	srv := startTestServer(t, port)
	defer stopTestServer(t, srv, port)

	result := GetAccountz(nil)
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if isErrorResponse(response) {
		t.Fatalf("Expected success, got error: %s", response)
	}

	var accountz map[string]interface{}
	if err := json.Unmarshal([]byte(response), &accountz); err != nil {
		t.Fatalf("Failed to parse Accountz JSON: %v", err)
	}

	if _, exists := accountz["accounts"]; !exists {
		t.Error("Expected 'accounts' field in Accountz response")
	}
}

// Test GetAccountz without server
func TestGetAccountz_ServerNotRunning(t *testing.T) {
	serverMu.Lock()
	currentPort = 99999
	serverMu.Unlock()

	result := GetAccountz(nil)
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if !isErrorResponse(response) {
		t.Fatal("Expected error when server not running")
	}
}

// Test GetVarz with server running
func TestGetVarz_ServerRunning(t *testing.T) {
	port := 14231
	srv := startTestServer(t, port)
	defer stopTestServer(t, srv, port)

	result := GetVarz()
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if isErrorResponse(response) {
		t.Fatalf("Expected success, got error: %s", response)
	}

	var varz map[string]interface{}
	if err := json.Unmarshal([]byte(response), &varz); err != nil {
		t.Fatalf("Failed to parse Varz JSON: %v", err)
	}

	// Check for critical Varz fields
	requiredFields := []string{"server_id", "version", "go", "host", "port"}
	for _, field := range requiredFields {
		if _, exists := varz[field]; !exists {
			t.Errorf("Expected '%s' field in Varz response", field)
		}
	}
}

// Test GetVarz without server
func TestGetVarz_ServerNotRunning(t *testing.T) {
	serverMu.Lock()
	currentPort = 99999
	serverMu.Unlock()

	result := GetVarz()
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if !isErrorResponse(response) {
		t.Fatal("Expected error when server not running")
	}
}

// Test GetGatewayz with server running
func TestGetGatewayz_ServerRunning(t *testing.T) {
	port := 14232
	srv := startTestServer(t, port)
	defer stopTestServer(t, srv, port)

	result := GetGatewayz(nil)
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if isErrorResponse(response) {
		t.Fatalf("Expected success, got error: %s", response)
	}

	var gatewayz map[string]interface{}
	if err := json.Unmarshal([]byte(response), &gatewayz); err != nil {
		t.Fatalf("Failed to parse Gatewayz JSON: %v", err)
	}
}

// Test GetGatewayz without server
func TestGetGatewayz_ServerNotRunning(t *testing.T) {
	serverMu.Lock()
	currentPort = 99999
	serverMu.Unlock()

	result := GetGatewayz(nil)
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if !isErrorResponse(response) {
		t.Fatal("Expected error when server not running")
	}
}

// Test concurrent access to monitoring endpoints
func TestConcurrentMonitoringCalls(t *testing.T) {
	port := 14233
	srv := startTestServer(t, port)
	defer stopTestServer(t, srv, port)

	// Make concurrent calls to different monitoring endpoints
	done := make(chan bool, 5)

	go func() {
		result := GetConnz(nil)
		C.free(unsafe.Pointer(result))
		done <- true
	}()

	go func() {
		result := GetSubsz(nil)
		C.free(unsafe.Pointer(result))
		done <- true
	}()

	go func() {
		result := GetRoutez()
		C.free(unsafe.Pointer(result))
		done <- true
	}()

	go func() {
		result := GetVarz()
		C.free(unsafe.Pointer(result))
		done <- true
	}()

	go func() {
		result := GetAccountz(nil)
		C.free(unsafe.Pointer(result))
		done <- true
	}()

	// Wait for all goroutines to complete
	for i := 0; i < 5; i++ {
		<-done
	}
}

// Test JSON marshaling edge cases
func TestJSONMarshaling_EmptyResults(t *testing.T) {
	port := 14234
	srv := startTestServer(t, port)
	defer stopTestServer(t, srv, port)

	// All endpoints should return valid JSON even with no data
	endpoints := []func() *C.char{
		func() *C.char { return GetConnz(nil) },
		func() *C.char { return GetSubsz(nil) },
		func() *C.char { return GetRoutez() },
		func() *C.char { return GetLeafz() },
		func() *C.char { return GetVarz() },
		func() *C.char { return GetAccountz(nil) },
		func() *C.char { return GetGatewayz(nil) },
	}

	for i, endpoint := range endpoints {
		result := endpoint()
		response := C.GoString(result)
		C.free(unsafe.Pointer(result))

		if isErrorResponse(response) {
			t.Errorf("Endpoint %d returned error: %s", i, response)
			continue
		}

		var data map[string]interface{}
		if err := json.Unmarshal([]byte(response), &data); err != nil {
			t.Errorf("Endpoint %d returned invalid JSON: %v", i, err)
		}
	}
}

// Test server state consistency
func TestServerStateConsistency(t *testing.T) {
	port1 := 14235
	port2 := 14236

	srv1 := startTestServer(t, port1)

	// Switch to different server
	serverMu.Lock()
	opts := &server.Options{
		Host: "127.0.0.1",
		Port: port2,
	}
	srv2, err := server.NewServer(opts)
	if err != nil {
		serverMu.Unlock()
		t.Fatalf("Failed to create server 2: %v", err)
	}
	go srv2.Start()
	if !srv2.ReadyForConnections(5 * time.Second) {
		serverMu.Unlock()
		t.Fatal("Server 2 not ready")
	}
	natsServers[port2] = srv2
	currentPort = port2
	serverMu.Unlock()

	// Test that we're getting data from port2
	result := GetVarz()
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if isErrorResponse(response) {
		t.Fatalf("Expected success from server 2, got error: %s", response)
	}

	var varz map[string]interface{}
	if err := json.Unmarshal([]byte(response), &varz); err != nil {
		t.Fatalf("Failed to parse Varz: %v", err)
	}

	if portFloat, ok := varz["port"].(float64); !ok || int(portFloat) != port2 {
		t.Errorf("Expected port %d, got %v", port2, varz["port"])
	}

	// Cleanup
	stopTestServer(t, srv1, port1)
	stopTestServer(t, srv2, port2)
}

// Test RegisterAccount with server running
func TestRegisterAccount_Success(t *testing.T) {
	port := 14237
	srv := startTestServer(t, port)
	defer stopTestServer(t, srv, port)

	accountName := C.CString("TEST_ACCOUNT_001")
	defer C.free(unsafe.Pointer(accountName))

	result := RegisterAccount(accountName)
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if isErrorResponse(response) {
		t.Fatalf("Expected success, got error: %s", response)
	}

	var account map[string]interface{}
	if err := json.Unmarshal([]byte(response), &account); err != nil {
		t.Fatalf("Failed to parse account JSON: %v", err)
	}

	// Validate response structure
	if name, ok := account["account"].(string); !ok || name != "TEST_ACCOUNT_001" {
		t.Errorf("Expected account name 'TEST_ACCOUNT_001', got %v", account["account"])
	}

	if _, exists := account["connections"]; !exists {
		t.Error("Expected 'connections' field in response")
	}

	if _, exists := account["subscriptions"]; !exists {
		t.Error("Expected 'subscriptions' field in response")
	}
}

// Test RegisterAccount with duplicate account
func TestRegisterAccount_Duplicate(t *testing.T) {
	port := 14238
	srv := startTestServer(t, port)
	defer stopTestServer(t, srv, port)

	accountName := C.CString("DUPLICATE_ACCOUNT")
	defer C.free(unsafe.Pointer(accountName))

	// Register account first time
	result1 := RegisterAccount(accountName)
	response1 := C.GoString(result1)
	C.free(unsafe.Pointer(result1))

	if isErrorResponse(response1) {
		t.Fatalf("First registration should succeed, got error: %s", response1)
	}

	// Try to register same account again (should fail)
	result2 := RegisterAccount(accountName)
	response2 := C.GoString(result2)
	C.free(unsafe.Pointer(result2))

	if !isErrorResponse(response2) {
		t.Fatal("Expected error for duplicate account registration")
	}

	if !strings.Contains(response2, "Failed to register account") {
		t.Errorf("Expected 'Failed to register account' error, got: %s", response2)
	}
}

// Test RegisterAccount without server
func TestRegisterAccount_ServerNotRunning(t *testing.T) {
	serverMu.Lock()
	currentPort = 99999
	serverMu.Unlock()

	accountName := C.CString("TEST_ACCOUNT")
	defer C.free(unsafe.Pointer(accountName))

	result := RegisterAccount(accountName)
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if !isErrorResponse(response) {
		t.Fatal("Expected error when server not running")
	}

	if !strings.Contains(response, "Server not running") {
		t.Errorf("Expected 'Server not running' error, got: %s", response)
	}
}

// Test RegisterAccount with null name
func TestRegisterAccount_NullName(t *testing.T) {
	port := 14239
	srv := startTestServer(t, port)
	defer stopTestServer(t, srv, port)

	result := RegisterAccount(nil)
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if !isErrorResponse(response) {
		t.Fatal("Expected error for null account name")
	}

	if !strings.Contains(response, "cannot be null") {
		t.Errorf("Expected 'cannot be null' error, got: %s", response)
	}
}

// Test LookupAccount with server running
func TestLookupAccount_Success(t *testing.T) {
	port := 14240
	srv := startTestServer(t, port)
	defer stopTestServer(t, srv, port)

	// Register an account first
	accountName := C.CString("LOOKUP_TEST_ACCOUNT")
	defer C.free(unsafe.Pointer(accountName))

	registerResult := RegisterAccount(accountName)
	C.free(unsafe.Pointer(registerResult))

	// Now lookup the account
	result := LookupAccount(accountName)
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if isErrorResponse(response) {
		t.Fatalf("Expected success, got error: %s", response)
	}

	var account map[string]interface{}
	if err := json.Unmarshal([]byte(response), &account); err != nil {
		t.Fatalf("Failed to parse account JSON: %v", err)
	}

	// Validate response structure
	if name, ok := account["account"].(string); !ok || name != "LOOKUP_TEST_ACCOUNT" {
		t.Errorf("Expected account name 'LOOKUP_TEST_ACCOUNT', got %v", account["account"])
	}

	if _, exists := account["total_subs"]; !exists {
		t.Error("Expected 'total_subs' field in lookup response")
	}
}

// Test LookupAccount for non-existent account
func TestLookupAccount_NotFound(t *testing.T) {
	port := 14241
	srv := startTestServer(t, port)
	defer stopTestServer(t, srv, port)

	accountName := C.CString("NONEXISTENT_ACCOUNT")
	defer C.free(unsafe.Pointer(accountName))

	result := LookupAccount(accountName)
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if !isErrorResponse(response) {
		t.Fatal("Expected error for non-existent account")
	}

	if !strings.Contains(response, "Account not found") {
		t.Errorf("Expected 'Account not found' error, got: %s", response)
	}
}

// Test LookupAccount without server
func TestLookupAccount_ServerNotRunning(t *testing.T) {
	serverMu.Lock()
	currentPort = 99999
	serverMu.Unlock()

	accountName := C.CString("TEST_ACCOUNT")
	defer C.free(unsafe.Pointer(accountName))

	result := LookupAccount(accountName)
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if !isErrorResponse(response) {
		t.Fatal("Expected error when server not running")
	}
}

// Test GetAccountStatz with server running
func TestGetAccountStatz_Success(t *testing.T) {
	port := 14242
	srv := startTestServer(t, port)
	defer stopTestServer(t, srv, port)

	// Register some test accounts
	account1 := C.CString("STATS_ACCOUNT_001")
	account2 := C.CString("STATS_ACCOUNT_002")
	defer C.free(unsafe.Pointer(account1))
	defer C.free(unsafe.Pointer(account2))

	reg1 := RegisterAccount(account1)
	reg2 := RegisterAccount(account2)
	C.free(unsafe.Pointer(reg1))
	C.free(unsafe.Pointer(reg2))

	// Get statistics for all accounts
	result := GetAccountStatz(nil)
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if isErrorResponse(response) {
		t.Fatalf("Expected success, got error: %s", response)
	}

	var statz map[string]interface{}
	if err := json.Unmarshal([]byte(response), &statz); err != nil {
		t.Fatalf("Failed to parse AccountStatz JSON: %v", err)
	}

	// Validate response structure
	if _, exists := statz["server_id"]; !exists {
		t.Error("Expected 'server_id' field in response")
	}

	if _, exists := statz["now"]; !exists {
		t.Error("Expected 'now' field in response")
	}

	if accounts, ok := statz["accounts"].([]interface{}); !ok {
		t.Error("Expected 'accounts' array in response")
	} else if len(accounts) == 0 {
		t.Error("Expected at least one account in statistics")
	}
}

// Test GetAccountStatz with account filter
func TestGetAccountStatz_WithFilter(t *testing.T) {
	port := 14243
	srv := startTestServer(t, port)
	defer stopTestServer(t, srv, port)

	// Register a test account
	accountName := C.CString("FILTERED_ACCOUNT")
	defer C.free(unsafe.Pointer(accountName))

	reg := RegisterAccount(accountName)
	C.free(unsafe.Pointer(reg))

	// Get statistics for specific account
	result := GetAccountStatz(accountName)
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if isErrorResponse(response) {
		t.Fatalf("Expected success, got error: %s", response)
	}

	var statz map[string]interface{}
	if err := json.Unmarshal([]byte(response), &statz); err != nil {
		t.Fatalf("Failed to parse filtered AccountStatz JSON: %v", err)
	}

	// Should have accounts array
	if _, exists := statz["accounts"]; !exists {
		t.Error("Expected 'accounts' field in filtered response")
	}
}

// Test GetAccountStatz without server
func TestGetAccountStatz_ServerNotRunning(t *testing.T) {
	serverMu.Lock()
	currentPort = 99999
	serverMu.Unlock()

	result := GetAccountStatz(nil)
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if !isErrorResponse(response) {
		t.Fatal("Expected error when server not running")
	}

	if !strings.Contains(response, "Server not running") {
		t.Errorf("Expected 'Server not running' error, got: %s", response)
	}
}

// TestGetServerID_Success tests getting the server ID when server is running.
func TestGetServerID_Success(t *testing.T) {
	port := 14241
	srv := startTestServer(t, port)
	defer stopTestServer(t, srv, port)

	result := GetServerID()
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if isErrorResponse(response) {
		t.Fatalf("Expected success, got error: %s", response)
	}

	// Server ID should be a non-empty string (UUID format)
	if response == "" {
		t.Error("Expected non-empty server ID")
	}

	t.Logf("Server ID: %s", response)
}

// TestGetServerID_ServerNotRunning tests getting server ID when server is not running.
func TestGetServerID_ServerNotRunning(t *testing.T) {
	// Don't start a server
	currentPort = 14242

	result := GetServerID()
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if !isErrorResponse(response) {
		t.Errorf("Expected error response, got: %s", response)
	}

	if !strings.Contains(response, "Server not running") {
		t.Errorf("Expected 'Server not running' error, got: %s", response)
	}
}

// TestGetServerName_Success tests getting the server name when server is running.
func TestGetServerName_Success(t *testing.T) {
	port := 14243
	srv := startTestServer(t, port)
	defer stopTestServer(t, srv, port)

	result := GetServerName()
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if isErrorResponse(response) {
		t.Fatalf("Expected success, got error: %s", response)
	}

	// Server name might be empty if not configured, which is valid
	t.Logf("Server name: '%s' (empty is valid if not configured)", response)
}

// TestGetServerName_ServerNotRunning tests getting server name when server is not running.
func TestGetServerName_ServerNotRunning(t *testing.T) {
	// Don't start a server
	currentPort = 14244

	result := GetServerName()
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if !isErrorResponse(response) {
		t.Errorf("Expected error response, got: %s", response)
	}

	if !strings.Contains(response, "Server not running") {
		t.Errorf("Expected 'Server not running' error, got: %s", response)
	}
}

// TestIsServerRunning_True tests when server is actually running.
func TestIsServerRunning_True(t *testing.T) {
	port := 14245
	srv := startTestServer(t, port)
	defer stopTestServer(t, srv, port)

	result := IsServerRunning()
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if response != "true" {
		t.Errorf("Expected 'true', got: %s", response)
	}

	t.Log("Server is running: true")
}

// TestIsServerRunning_False tests when server is not running.
func TestIsServerRunning_False(t *testing.T) {
	// Don't start a server
	currentPort = 14246

	result := IsServerRunning()
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if response != "false" {
		t.Errorf("Expected 'false', got: %s", response)
	}

	t.Log("Server is running: false")
}

// TestIsServerRunning_AfterShutdown tests that IsServerRunning returns false after shutdown.
func TestIsServerRunning_AfterShutdown(t *testing.T) {
	port := 14247
	srv := startTestServer(t, port)

	// Verify it's running first
	result := IsServerRunning()
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if response != "true" {
		t.Errorf("Expected 'true' before shutdown, got: %s", response)
	}

	// Shutdown the server
	stopTestServer(t, srv, port)

	// Now it should return false
	result = IsServerRunning()
	response = C.GoString(result)
	C.free(unsafe.Pointer(result))

	if response != "false" {
		t.Errorf("Expected 'false' after shutdown, got: %s", response)
	}

	t.Log("Server correctly reports not running after shutdown")
}

// TestWaitForReadyState_Success tests waiting for server to be ready.
func TestWaitForReadyState_Success(t *testing.T) {
	port := 14248
	srv := startTestServer(t, port)
	defer stopTestServer(t, srv, port)

	result := WaitForReadyState(C.int(5))
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if response != "true" {
		t.Errorf("Expected 'true' (ready), got: %s", response)
	}

	t.Log("Server is ready: true")
}

// TestWaitForReadyState_Timeout tests timeout behavior.
func TestWaitForReadyState_ServerNotRunning(t *testing.T) {
	// Don't start a server
	currentPort = 14249

	result := WaitForReadyState(C.int(1))
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if !isErrorResponse(response) {
		t.Errorf("Expected error response, got: %s", response)
	}

	if !strings.Contains(response, "Server not running") {
		t.Errorf("Expected 'Server not running' error, got: %s", response)
	}
}

// TestIsJetStreamEnabled_WithoutJetStream tests when JetStream is not enabled.
func TestIsJetStreamEnabled_WithoutJetStream(t *testing.T) {
	port := 14250
	srv := startTestServer(t, port)
	defer stopTestServer(t, srv, port)

	result := IsJetStreamEnabled()
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if response != "false" {
		t.Errorf("Expected 'false' (JetStream not enabled), got: %s", response)
	}

	t.Log("JetStream enabled: false")
}

// TestIsJetStreamEnabled_WithJetStream tests when JetStream is enabled.
func TestIsJetStreamEnabled_WithJetStream(t *testing.T) {
	port := 14251

	// Create server with JetStream enabled
	opts := &server.Options{
		Host:      "127.0.0.1",
		Port:      port,
		JetStream: true,
	}

	srv, err := server.NewServer(opts)
	if err != nil {
		t.Fatalf("Failed to create NATS server: %v", err)
	}

	go srv.Start()

	if !srv.ReadyForConnections(5 * time.Second) {
		t.Fatal("Server did not become ready in time")
	}

	serverMu.Lock()
	natsServers[port] = srv
	currentPort = port
	serverMu.Unlock()

	defer stopTestServer(t, srv, port)

	result := IsJetStreamEnabled()
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if response != "true" {
		t.Errorf("Expected 'true' (JetStream enabled), got: %s", response)
	}

	t.Log("JetStream enabled: true")
}

// TestIsJetStreamEnabled_ServerNotRunning tests when server is not running.
func TestIsJetStreamEnabled_ServerNotRunning(t *testing.T) {
	// Don't start a server
	currentPort = 14252

	result := IsJetStreamEnabled()
	response := C.GoString(result)
	C.free(unsafe.Pointer(result))

	if !isErrorResponse(response) {
		t.Errorf("Expected error response, got: %s", response)
	}

	if !strings.Contains(response, "Server not running") {
		t.Errorf("Expected 'Server not running' error, got: %s", response)
	}
}
