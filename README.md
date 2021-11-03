# DuetPrintFarm

This is the backend application for print farms running Duet boards written in C# / ASP.NET 5.
It is licensed under the terms of the GPLv3.

## Configuration files

Apart from the default `appsettings.json` file, there are two JSON files

- `jobQueue.json`: This file stores the completed and pending jobs
- `printers.json`: This file stores the configured printers

The names of these two files and the target directory for incoming G-code files can be defined in `appsettings.json`.

## User Interface

See [DuetPrintFarmUI](https://github.com/Duet3D/DuetPrintFarmUI) for the web frontend to drive this application.

## General Workflow

This application mimics the DSF endpoint `PUT /machine/file` to upload files from third-party applications using the [DSF RESTful API](https://github.com/Duet3D/DuetSoftwareFramework/wiki/REST-API).
Whenever a new file is uploaded, it is stored in `GCodesDirectory` (defaults to `gcodes`).

As soon as the upload is complete, the file will be added to the job queue.
Every printer that is idle will start waiting for incoming print jobs on this queue in round-robin order so that every machine is equally used.

- Before a printer starts processing jobs, this application attempts to run `queue-start.g` on the remote machine.
This lets users prepare the initial print environment before more jobs are started.
Once `queue-start.g` has been processed, the G-code file is uploaded and it is started by `M32`.

- When the printer has finished, the printer instance will check if there is a following G-code file available for processing.
If it is, the  step as before - except with `queue-intermediate.g` instead of `queue-start.g` is repeated.
For most configurations this macro file should show a message prompt using `M291` and ask the user to remove the finished print object.

- In case no more jobs are available when the print has finished, `queue-end.g` is executed.
This file may be used to turn off the heaters and to display a final prompt asking the user to remove the printed object.

