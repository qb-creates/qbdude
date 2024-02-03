# QB.DUDE

QB.DUDE is a program for uploading program data to the on chip memories of Microship's <a href="https://en.wikipedia.org/wiki/AVR_microcontrollers">AVR microcontrollers.</a>. This utility will upload program data to AVR microcontrollers that are running a QB.Creates bootloader. Supported microcontrollers and QB.Creates bootloaders can be found <a href="https://github.com/qb-creates/avr-bootloaders">here</a>.

All QB.DUDE releases can be found here: https://github.com/qb-creates/qbdude/releases

## Using QB.DUDE 
QB.DUDE is a command line application. Run the ```qbdude``` executable in the command line without any arguments for a list of commands and options. Run the ```qbdude``` executable in the command line with a valid command followed by -h option to give more information about that command.

<img src = "images/qbdude.png">


The command to upload a HEX file into your AVR microcontroller looks like this:
```
qbdude upload -p [PARTNUMBER] -C [COMPORT] -F [HEXFILEPATH]
```

<br> 

To upload program data to an ATmega128 microcontroller connected to COM5 with a HEX file called firmware.hex, you would run the following command:
```
qbdude upload -p m128 -C COM5 -F firmware.hex
```

### Part Numbers
Supported partnumbers can be found in the microcontrollers.json file. Part numbers are the key for each object in the array. 
Alternatively, you can run the following command to get all supported part numbers

<img src = "images/partnumber.png">

Part number Example:
```
qbdude partnumber
```

### Comports
Run the ```comport``` command without an options to view the available comports on your system. All comport names will be printed along with their description.

<img src = "images/comport.png">

Comport example:
```
qbdude comport
```
