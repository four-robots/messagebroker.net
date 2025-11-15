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
