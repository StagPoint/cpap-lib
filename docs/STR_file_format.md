# Data Format: STR.edf File

The STR.edf file is a [European Data Format](https://en.wikipedia.org/wiki/European_Data_Format) file that contains 81 signals representing what is essentially a [vertical table](https://en.wikipedia.org/wiki/Partition_(database)#Vertical_partitioning) of values reflecting the settings 
and statistics for each recorded day. 

### General Information

| Signal           | Description                                                                                           |
|------------------|-------------------------------------------------------------------------------------------------------|
| Date             | The "ResMed Date" on which the recording starts                                                       | 
| MaskOn           | An array of offsets (in seconds) from the start time of the file indicating the start of each session | 
| MaskOff          | An array of offsets (in seconds) from the start time of the file indicating the end of each session   | 
| MaskEvents       | The number of (MaskOn, MaskOff) events per day.                                                       | 
| Duration         | The combined duration (in minutes) of all sessions on the given day.                                  | 
| OnDuration       | TBD                                                                                                   | 
| PatientHours     | TBD                                                                                                   | 
| Mode             | The therapy mode used (CPAP, APAP, BiLevel, ASV, etc)                                                 | 

### Event Information

| Signal           | Description                                                                                                                                                                                                                   |
|------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| AHI              | [Apnea-Hypopnea Index](https://en.wikipedia.org/wiki/Apnea%E2%80%93hypopnea_index), the average number of [Apnea](https://en.wikipedia.org/wiki/Apnea) or [Hypopnea](https://en.wikipedia.org/wiki/Hypopnea) events per hour. | 
| HI               | Hypopnea Index - The average number of [Hypopnea](https://en.wikipedia.org/wiki/Hypopnea) events that occurred per hour                                                                                                       | 
| AI               | Apnea Index - The Average number of [Apnea](https://en.wikipedia.org/wiki/Apnea) events that occurred per hour.                                                                                                               | 
| OAI              | Obstructive Apnea Index - The average number of Obstructive Apnea events that occurred per hour.                                                                                                                              | 
| CAI              | Clear Airway Index - The number of Clear Airway (or [Central Apnea](https://en.wikipedia.org/wiki/Central_sleep_apnea)) events that occurred per hour.                                                                        | 
| UAI              | Unclassified Apnea Index - The number of Apnea events that could not be classified that occurred per hour.                                                                                                                    | 
| RIN              | Respiratory Disturbance Index - The number of [Respiratory Effort Related Arousal](https://en.wikipedia.org/wiki/Respiratory_disturbance_index) events that occurred per hour.                                                | 
| CSR              | Cheyne–Stokes Respiration Index - The number of [Cheyne–Stokes Respiration](https://en.wikipedia.org/wiki/Cheyne%E2%80%93Stokes_respiration) events that occurred per hour.                                                   |

## Machine Settings 

### General Settings

| Signal           | Data Type   | Description                                           |
|------------------|-------------|-------------------------------------------------------|
| S.RampEnable     | True/False  | Indicates whether the Ramp mode is enabled            |
| S.RampTime       | Number      | The length of the ramp period, in minutes             |
| S.SmartStart     | True/False  |                                                       |
| S.PtAccess       | Plus/On     | Essentials mode                                       |
| S.ABFilter       | Yes/No      | Whether antibacterial filters are installed           |
| S.Mask           | Enum        | Can be set to Unknown, Pillows, Nasal, or Full Face   |
| S.Tube           | Number      | When enabled, contains the Tube Temperature set point |
| S.ClimateControl | Manual/Auto | Whether Climate Control is set to Manual or Auto mode |
| S.HumEnable      | True/False  | Whether the humidifier is enabled                     |
| S.HumLevel       | Number      | The Humidity Level set point                          |
| S.TempEnable     | True/False  | TBD                                                   |
| S.Temp           | Number      | The temperature set point                             |
| HeatedTube       | True/False  | Whether a heated tube is attached                     |
| Humidifier       | True/False  | Whether a humidifier unit is attached                 |

### CPAP Mode settings

These settings are used when the machine is operating in CPAP mode

| Signal           | Data Type | Description                                                             |
|------------------|-----------|-------------------------------------------------------------------------|
| S.C.StartPress   | Number    | The starting pressure for the Ramp, if enabled                          |
| S.C.Press        | Number    | The constant pressure that will be delivered (except during ramp times) |

### AutoSet Settings

These settings are used when the machine is operating in AutoSet mode (see [here](https://www.resmed.com.au/healthcare-professionals/airsolutions/innovation-and-technology) for more information)

| Signal           | Data Type     | Description                                                                   |
|------------------|---------------|-------------------------------------------------------------------------------|
| S.AS.Comfort     | Standard/Soft | Indicates the speed at which pressure increases during AutoSet mode operation |
| S.AS.StartPress  | Number        | The starting pressure during the Ramp period, if enabled                      |
| S.AS.MaxPress    | Number        | The maximum pressure that will be delivered                                   |
| S.AS.MinPress    | Number        | The minimum pressure that will be delivered                                   |


### EPR settings

These settings are used when Expiratory Pressure Relief (EPR) is enabled.

| Signal           | Data Type                   | Description                                                                              |
|------------------|-----------------------------|------------------------------------------------------------------------------------------|
| S.EPR.ClinEnable | True/False                  | TBD - Maybe whether EPR is enabled through the Clinician's Menu?                         |
| S.EPR.EPREnable  | True/False                  | Indicates whether EPR is enabled                                                         |
| S.EPR.Level      | Number (1-3)                | The amount of pressure drop (in cmHO2) that will occur when exhaling                     |
| S.EPR.EPRType    | Off / Ramp Only / Full Time | When EPR is active. It can be off, active only during Ramp time, or enabled at all times |


### Statistics

| Signal           | Description |
|------------------|-------------|
| BlowPress.95     |             | 
| BlowPress.5      |             | 
| Flow.95          |             | 
| Flow.5           |             | 
| BlowFlow.50      |             | 
| AmbHumidity.50   |             | 
| HumTemp.50       |             | 
| HTubeTemp.50     |             | 
| HTubePow.50      |             | 
| HumPow.50        |             | 
| SpO2.50          |             | 
| SpO2.95          |             | 
| SpO2.Max         |             | 
| SpO2Thresh       |             | 
| MaskPress.50     |             | 
| MaskPress.95     |             | 
| MaskPress.Max    |             | 
| TgtIPAP.50       |             | 
| TgtIPAP.95       |             | 
| TgtIPAP.Max      |             | 
| TgtEPAP.50       |             | 
| TgtEPAP.95       |             | 
| TgtEPAP.Max      |             | 
| Leak.50          |             | 
| Leak.95          |             | 
| Leak.70          |             | 
| Leak.Max         |             | 
| MinVent.50       |             | 
| MinVent.95       |             | 
| MinVent.Max      |             | 
| RespRate.50      |             | 
| RespRate.95      |             | 
| RespRate.Max     |             | 
| TidVol.50        |             | 
| TidVol.95        |             | 
| TidVol.Max       |             | 

### Fault Information

| Signal           | Description |
|------------------|-------------|
| Fault.Device     | TBD         | 
| Fault.Alarm      | TBD         | 
| Fault.Humidifier | TBD         | 
| Fault.HeatedTube | TBD         | 
| Crc16            | TBD         |