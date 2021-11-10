# Bisign2Bikey
Extracts the BI key from BI signature files for DayZ and ArmA titles, currently tested with Bohemia Interactive's latest version 3 keys/signatures. This is just a simple pattern scan, which checks for a separator between the actual signature and the key which is stored at the head of the signature file.

# Usage
There are various ways to use Bisign2Bikey, any of the following will produce a 'Keys' folder in the source directory, **not** the Bisign2Bikey.exe directory.

* Drag any .bisign file(s) over top of the executable
* Drag addons folder(s) of mod over top of the executable
* Drag @ModName folder(s) over top of the executable

![Example Output](./img/example1.png)