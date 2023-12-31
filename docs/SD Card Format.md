﻿# SD Card Files and Folder Structure

<!-- TOC -->
* [SD Card Files and Folder Structure](#sd-card-files-and-folder-structure)
    * [Example](#example-)
    * [Identification.tgt](#identificationtgt)
    * [STR.edf](#stredf)
    * [DATALOG Folder](#datalog-folder)
<!-- TOC -->

### Example 
```
Root Folder/
├── Identification.tgt
├── STR.edf
└── DATALOG/
    ├── 20230821/
    │   ├── 20230822_001107_EVE.edf
    │   ├── 20230822_001118_BRP.edf
    │   ├── 20230822_001119_PLD.edf
    │   ├── 20230822_001119_SAD.edf
    │   └── ...
    ├── 20230822/
    │   ├── 20230822_224916_EVE.edf
    │   ├── 20230822_224924_BRP.edf
    │   ├── 20230822_224924_PLD.edf
    │   ├── 20230822_224924_SAD.edf
    │   └── ...
    └── ...
```

### Identification.tgt

This is a two-column text file with a number of machine-specific fields.

| Key  | Example Value       | Meaning                                  |
|------|---------------------|------------------------------------------|
| #SRN | XXXXXXXXXXX         | Serial number                            |
| #PNA | AirSense_10_AutoSet | Text description of the model of machine |
| #PCD | 37028               | The Model Number                         |

| Key  | Example Value                   | Meaning |
|------|---------------------------------|---------|
| #IMF | 0001                            | TBD     |
| #VIR | 0064                            | TBD     |
| #RIR | 0064                            | TBD     |
| #PVR | 0064                            | TBD     |
| #PVD | 001A                            | TBD     |
| #CID | CXXXX-XXX-XXX-XXX-XXX-XXX-XXX   | TBD     |
| #RID | 000D                            | TBD     |
| #VID | 0027                            | TBD     |
| #SID | SXXXX-XXXX                      | TBD     |
| #PCB | (90)R370-7518(91)T1(21)97141060 | TBD     |
| #MID | 0024                            | TBD     |
| #FGT | 24_M36_V39                      | TBD     |
| #BID | SX577-0200                      | TBD     |


### STR.edf

The STR.edf file is a [European Data Format](https://en.wikipedia.org/wiki/European_Data_Format) file that contains 81 signals representing what is essentially a [vertical table](https://en.wikipedia.org/wiki/Partition_(database)#Vertical_partitioning) of values reflecting the settings
and statistics for each recorded day.

It is discussed in more detail [on this page](STR_file_format.md).

### DATALOG Folder

Underneath the DATALOG folder is another set of folders, one for each day recorded. Each folder will be named for the day whose data is stored within. 

Within each of those dated subfolders will be a set of [EDF](https://en.wikipedia.org/wiki/European_Data_Format) files that contain the recorded data for each session recorded on that day. 
<br/>These files will each have a filename that conforms to the following format: *yyyyMMdd_HHmmss_[Type].edf*

| Example Path     | Example Filename        | Type | Description                                                                                                                                                                                                                                               |
|:-----------------|-------------------------|------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| DATALOG/20230821 | 20230822_001107_EVE.edf | EVE  | There will only be one of these per folder, and it contains all of the events (Apnea, Hypopnea, CSR, etc) that were flagged during the entire day's recording sessions. The filename contains the date and time of the first session recorded on this day |
| DATALOG/20230821 | 20230822_001118_BRP.edf | BRP  | There will be one of these files for every recorded session. It contains the high-resolution Flow and Pressure data for the session.                                                                                                                      |
| DATALOG/20230821 | 20230822_001119_PLD.edf | PLD  | There will be one of these files for every recorded session. It contains the low-resolution data for the recording session (Flow Limit, Mask Pressure, etc)                                                                                               |
| DATALOG/20230821 | 20230822_001118_SAD.edf | SAD  | There will be one of these files for every recorded session. It contains Oximetry data (Blood Oxygen Saturation %, Pulse Rate) for the recorded session. If no pulse oximeter device is attached, the signal data will contain negative values.           |

