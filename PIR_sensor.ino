#include <WiFi.h>
#include <HTTPClient.h>
#include <ArduinoJson.h>

const char* ssid = "YOUR_WIFI_SSID";
const char* password = "YOUR_WIFI_PASSWORD";
const char* serverURL = "http://YOUR_SERVER_IP:5000/api/motion-events";

const int pirPin = 13; // PIR sensor pin

void setup() {
  Serial.begin(115200);
  pinMode(pirPin, INPUT);
  
  WiFi.begin(ssid, password);
  while (WiFi.status() != WL_CONNECTED) {
    delay(1000);
    Serial.println("Connecting to WiFi...");
  }
  Serial.println("Connected to WiFi");
}

void loop() {
  int motionDetected = digitalRead(pirPin);
  
  if (motionDetected == HIGH) {
    sendMotionEvent();
    delay(5000); // Avoid spam
  }
  delay(100);
}

void sendMotionEvent() {
  if (WiFi.status() == WL_CONNECTED) {
    HTTPClient http;
    http.begin(serverURL);
    http.addHeader("Content-Type", "application/json");
    
    DynamicJsonDocument doc(200);
    doc["sensorId"] = "ESP32_PIR_01";
    doc["eventType"] = "motion_detected";
    doc["location"] = "Living Room";
    
    String payload;
    serializeJson(doc, payload);
    
    int httpResponseCode = http.POST(payload);
    
    if (httpResponseCode > 0) {
      Serial.println("Motion event sent successfully");
    } else {
      Serial.println("Error sending motion event");
    }
    
    http.end();
  }
}