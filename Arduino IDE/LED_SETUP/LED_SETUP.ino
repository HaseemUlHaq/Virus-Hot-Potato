#include <FastLED.h>

#define LED_PIN     8
#define NUM_LEDS    200
#define BUTTON_PIN  0

CRGB leds[NUM_LEDS];
bool isRed = true;

void setColor(CRGB color) {
  FastLED.clear();
  for(int i = 0; i < NUM_LEDS; i += 4) {
    leds[i] = color;
  }
  FastLED.show();
}

void setup() {
  pinMode(BUTTON_PIN, INPUT_PULLUP);
  FastLED.addLeds<WS2811, LED_PIN, RGB>(leds, NUM_LEDS);
  FastLED.setBrightness(255);
  setColor(CRGB::Green); // Green = Red on this strip
}

void loop() {
  if (digitalRead(BUTTON_PIN) == LOW) {
    isRed = !isRed;
    if (isRed) {
      setColor(CRGB::Green); // Red
    } else {
      setColor(CRGB::Red); // Green
    }
    delay(300);
  }
}
