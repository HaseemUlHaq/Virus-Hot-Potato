#include <WiFi.h>
#include <WiFiUdp.h>

// ─── CONFIG ───────────────────────────────────────
const char* ssid      = "dsv-extrality-lab";
const char* password  = "expiring-unstuck-slider";
const char* unityIP   = "10.204.0.84";
const int   udpPort   = 5006;  // SAME PORT for both button and breath
const int   buttonPin = 12;
const int   soundPin  = 6;    // Sound sensor analog input
// ──────────────────────────────────────────────────

// ─── SOUND SENSOR SETTINGS ─────────────────────────
const int SENSITIVITY_MARGIN = 1000;
const int SAMPLES_TO_CONFIRM = 3;
const unsigned long BLOW_COOLDOWN = 500;  // ms between allowed blows

int quietBaseline = 0;
int blowThreshold = 0;
int aboveThresholdCount = 0;
unsigned long lastBlowTime = 0;
int peakInWindow = 0;
unsigned long lastPrintTime = 0;
const unsigned long PRINT_INTERVAL = 100;

// ──────────────────────────────────────────────────

WiFiUDP udp;

void setup() {
    Serial.begin(115200);
    delay(1000);
    
    pinMode(buttonPin, INPUT_PULLUP);
    // Sound sensor is analog, no pin mode needed
    
    Serial.println("\n\n╔════════════════════════════════════════╗");
    Serial.println("║  ESP32 Button + Breath Sensor          ║");
    Serial.println("╚════════════════════════════════════════╝\n");

    // ─── WiFi Connection ──────────────────────────────────
    WiFi.begin(ssid, password);
    Serial.print("[ESP32] Connecting to WiFi");

    int attempts = 0;
    while (WiFi.status() != WL_CONNECTED && attempts < 20) {
        delay(500);
        Serial.print(".");
        attempts++;
    }

    if (WiFi.status() != WL_CONNECTED) {
        Serial.println("\n[ESP32] ✗ WiFi failed!");
        return;
    }

    Serial.println("\n[ESP32] ✓ WiFi Connected!");
    Serial.println("[ESP32] ESP32 IP : " + WiFi.localIP().toString());
    Serial.println("[ESP32] Unity IP : " + String(unityIP));
    Serial.println("[ESP32] Port     : " + String(udpPort));
    
    udp.begin(udpPort);

    // ─── Calibrate Sound Sensor ──────────────────────────
    Serial.println("\n[SOUND] Calibrating baseline (keep quiet for 3 seconds)...\n");
    delay(500);
    
    long sum = 0;
    int maxQuiet = 0;
    const int CAL_SAMPLES = 100;
    
    for (int i = 0; i < CAL_SAMPLES; i++)
    {
        int raw = analogRead(soundPin);
        sum += raw;
        if (raw > maxQuiet) maxQuiet = raw;
        delay(30);
    }
    
    quietBaseline = sum / CAL_SAMPLES;
    blowThreshold = maxQuiet + SENSITIVITY_MARGIN;
    
    Serial.println("╔════════════════════════════════════════╗");
    Serial.print("║ Quiet Average:     ");
    Serial.print(quietBaseline);
    Serial.println();
    Serial.print("║ Quiet Peak (max):  ");
    Serial.print(maxQuiet);
    Serial.println();
    Serial.print("║ Blow Threshold:    ");
    Serial.print(blowThreshold);
    Serial.println();
    Serial.println("╚════════════════════════════════════════╝\n");
    
    Serial.println("[ESP32] Ready - Press Button or Blow!");
}

void loop() {
    // ─── Button Check ─────────────────────────────────────
    bool buttonState = digitalRead(buttonPin);
    if (buttonState == LOW) {
        Serial.println("[BUTTON] ✓ Button pressed - sending...");
        sendUDP("BUTTON_PRESSED");
        delay(300);
    }

    // ─── Sound Sensor Check ───────────────────────────────
    int rawValue = analogRead(soundPin);
    
    // Track peak within print window
    if (rawValue > peakInWindow) peakInWindow = rawValue;
    
    // Check if above threshold
    if (rawValue > blowThreshold)
    {
        aboveThresholdCount++;
        
        // Confirmed blow! (multiple samples above threshold in a row)
        if (aboveThresholdCount >= SAMPLES_TO_CONFIRM)
        {
            unsigned long now = millis();
            if (now - lastBlowTime > BLOW_COOLDOWN)
            {
                // 🌬️ BLOW DETECTED!
                Serial.println();
                Serial.print("[BREATH] ✓ BLOW DETECTED! Peak: ");
                Serial.print(rawValue);
                Serial.println();
                sendUDP("BLOW");
                
                lastBlowTime = now;
            }
            aboveThresholdCount = 0;  // Reset for next blow
        }
    }
    else
    {
        // Reset counter when below threshold
        aboveThresholdCount = 0;
    }
    
    // Periodic status print
    if (millis() - lastPrintTime >= PRINT_INTERVAL)
    {
        lastPrintTime = millis();
        
        Serial.print("[SOUND] Peak: ");
        Serial.print(peakInWindow);
        Serial.print(" | Threshold: ");
        Serial.print(blowThreshold);
        Serial.print(" | Status: ");
        
        if (peakInWindow > blowThreshold)
        {
            Serial.println("⚡ Spike");
        }
        else
        {
            Serial.println("─ quiet");
        }
        
        peakInWindow = 0;  // Reset peak tracker
    }
}

void sendUDP(const char* message) {
    if (WiFi.status() == WL_CONNECTED) {
        udp.beginPacket(unityIP, udpPort);
        udp.print(message);
        udp.endPacket();
        Serial.println("[UDP] ✓ Sent '" + String(message) + "' to " + String(unityIP) + ":" + String(udpPort));
    } else {
        Serial.println("[UDP] ✗ WiFi not connected! Reconnecting...");
        WiFi.begin(ssid, password);
    }
}

//