# AliveChecker

This project is meant to be a simple Command line application or an Azure Function app that receive in input a CSV file with a list of "CodiceFiscale" used to check if they are still alive or not.

## Feature of the Client

Business Features already developed:

- The File is parsed and every row is Checked over the [anpr](https://github.com/italia/anpr) platform
- The client is authorized and each call is signed with a private key configured in the system
- If the given Authorization token expires a new one is calculated and used in the following calls
- If the server doesn't give a expected answer the Item is set to be retried later
- The application write the results in an output CSV file (CF,InVita,DataDecesso,IdOperationeANPR,DataControllo,DescrizioneStato)
- The output file name is calculated from the input file appending the _out.csv file
- A log with all performed operations is produced in the same folder of the application
- A SqlLite Database called Checker.db is used to track all operations and at the end it contains all data, answers and status.
- If the setting "InitializeDb" is false the Checked.db database is preserved and all data is appended.
- Open issues and future improvements can be found [here](https://dev.azure.com/adesso-it/AliveChecker/_workitems/recentlyupdated/)