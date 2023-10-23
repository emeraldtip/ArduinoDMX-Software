/*
 * This thing writes the received data from the serial input onto a tft lcd thing (cause I have no dmx fixtures at home)
 */


#include <Adafruit_GFX.h>    // Core graphics library
#include <Adafruit_TFTLCD.h> // Hardware-specific library

#define LCD_CS A3
#define LCD_CD A2
#define LCD_WR A1
#define LCD_RD A0
// optional
#define LCD_RESET A4


#define  BLACK   0x0000


Adafruit_TFTLCD tft(LCD_CS, LCD_CD, LCD_WR, LCD_RD, LCD_RESET);

int i = 0;

void setup() {
  // put your setup code here, to run once:
  tft.reset();
  tft.begin(tft.readID());
  tft.fillScreen(BLACK);
  tft.println("Bruh moment:");
  Serial.setTimeout(1000);
  Serial.begin(115200);
}

byte incomingByte[512];

void loop() {
  if (Serial.available() > 0) { 
    // read the incoming byte:
    Serial.readBytesUntil(90,incomingByte, 512);
    
    for (int e = 0; e<512; e++)
    {
      if (incomingByte[e]!=0)
      {
         tft.println("Setting " + String(e+1) + " to " + String(incomingByte[e]));
         i++;
      }
    }
    
    
    
    if (i>60)
    {
      tft.setCursor(0,0);
      i = 0;
      tft.fillScreen(BLACK);
    }
  }
}
