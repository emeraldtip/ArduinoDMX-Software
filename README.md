# Arduino-DMX sofware

This is an absolutely horrible DMX console that is pretty much completely useless, slow etc. (But it kinda works)

Update: I managed to do a bit of testing and it actually seems to work fairly decently (at least with like 3 fixtures) **(NEEDS FURTHER TESTING)**. 

This repo features 2 sketches in the arduino folder:
- The [serialdebugger](https://github.com/emeraldtip/ArduinoDMX-Software/blob/master/arduino/serialdebugger/serialdebugger.ino) sketch, which writes the channel being changed and the value it is being provided to a TFT LCD screen. It has some issues (I'm guessing due to the screen having to refresh, which causes lag, which knocks the serial read of the arduino out of alignment with the bytes being sent by the program on the PC)
- The [dmxtest](https://github.com/emeraldtip/ArduinoDMX-Software/blob/master/arduino/dmxtest/dmxtest.ino) sketch, which actually sends data to an XLR jack. This seems to be working fairly well and I haven't had issues with it so far. The arduino sends data out from pin 3 (pwm enabled) to a velleman [VMA432 module](https://www.velleman.eu/products/view/?id=439222), which uses an SN75176B driver chip.

Also the code is written in like 1-2 nights and is absolute dogshit

