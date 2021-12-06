# JSONDownloader - .NET 5.0 Console App 

## Features
- read in multiple JSON URLs as a semicolon separated string
- read in save folder to download the JSON files to
- verify that the URL is real
- verify that the URL actually exists
- download the JSON file
- save the JSON file to a given folder

## Notes
### Key assumptions
- If URL is valid and exists, it leads to valid JSON file.
- Valid URL includes HTTP/HTTPS scheme.

### Timeout
The app currently does not handle requests taking a long time in any way. An example solution would be to retry the request as a new task if the original is taking too long and if one of them succeeds to cancel the other(s).

### OS
The program was tested on Linux system only.

The program itself should work on both Windows and UNIX systems, however, the tests were written on and for Linux so test paths use ```/```.  While the slashes could be mitigated by using ```Path.Combine```, the OS restricted filename characters are harder to test as Windows is more restrictive.

### External libraries used
- [Moq](https://www.nuget.org/packages/Moq/4.16.1)
- [System.IO.Abstractions](https://www.nuget.org/packages/System.IO.Abstractions/14.0.3)
- [System.IO.Abstractions.TestingHelpers](https://www.nuget.org/packages/System.IO.Abstractions.TestingHelpers/14.0.3)
