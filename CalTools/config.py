
folders  = ["PRODUCTION EQUIPMENT","ENGINEERING EQUIPMENT","QUALITY EQUIPMENT", "Ref Only", "Removed from Service"]

#Prefix directory strings with r

#Database name
dbName = r"debug_Test Equipment Calibration List.db"

#Location of Database
calListDir = r"\\artemis\Hardware Development Projects\Manufacturing Engineering\Test Equipment"

#Location of template files
tempFilesDir = r"\\artemis\Hardware Development Projects\Manufacturing Engineering\Test Equipment\Template Files"

#Folder that includes "Calibration Items" folder
calScansDir = r"\\artemis\Hardware Development Projects\Manufacturing Engineering\Test Equipment\Calibration Scans"

firstRun = True

#Certificate Report Template Cells
certificateFileName = "ReportTemplate.xlsx"
cManufacturer = "C5"
cModel = "C6"
cSerialNumber = "C7"
cDescription = "C8"
cCalibrationBox = "J12"
cVerificationBox = "J13"
cCalibrationDate = "D18"
cOperationDate = "D19"
cDueDate = "D20"
cProcedure = "I18"
cLocation = "I19"
cCertificateDate = "J42"
