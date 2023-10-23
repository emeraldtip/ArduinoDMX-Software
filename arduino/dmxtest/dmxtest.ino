#include <DmxSimple.h>
byte incomingBytes[512]; // for incoming serial data


void setup() {
  Serial.setTimeout(10);
  pinMode(13,OUTPUT);
  Serial.begin(115200); 
  DmxSimple.usePin(3);
  DmxSimple.maxChannel(512); 
}

void loop() {
  // send data only when you receive data:
  if (Serial.available() > 0) {
    // read the incoming data:
    Serial.readBytesUntil(90,incomingBytes, 512);
    for (int i = 0; i<512; i++)
    {
      DmxSimple.write(i+1,incomingBytes[i]);
    }
  }
}
