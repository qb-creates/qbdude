# QB.DUDE

QB.DUDE is a program for uploading program data to the on chip memories of Microship's <a href="https://en.wikipedia.org/wiki/AVR_microcontrollers">AVR microcontrollers.</a>. This utility will upload program data to AVR microcontrollers that are running a QB.Creates bootloader. Supported microcontrollers and QB.Creates bootloaders can be found <a href="https://github.com/qb-creates/avr-bootloaders">here</a>.

All QB.DUDE releases can be found here: https://github.com/qb-creates/qbdude/releases

## Using QB.DUDE 
QB.DUDE is a command line application. Run the ```qbdude``` executable in the command line without any arguments for a list of commands and options. Run the ```qbdude``` executable in the command line with a valid command followed by -h to get more information about that command.

![image](https://github.com/qb-creates/qbdude/assets/79540225/2c0f942d-3369-44a2-b7c0-bcf653aec816)


The command to upload your HEX file into your AVR microcontroller looks like this:
```
qbdude upload -p [PARTNUMBER] -C [COMPORT] -F [HEXFILEPATH]
```
For instance, to program an ATmega128 microcontroller connected to COM5 with a HEX file called firmware.hex, you would run the following command:
```
qbdude upload -p m128 -C COM5 -F firmware.hex
```
Supported partnumbers can be found in the microcontrollers.json file. Part numbers are the key for each object in the array. 
Alternatively, you can run the following command to get all supported part numbers
```
qbdude partnumber
```
![image](https://github.com/qb-creates/qbdude/assets/79540225/4a3cb8ff-6d25-479b-948c-3cc0735b8197)

