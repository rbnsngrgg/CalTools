import pickle, sys, openpyxl, sqlite3, listSearch, time
from os import remove, path, startfile, listdir, unlink # was import os
from shutil import copyfile, move, rmtree # was import shutil
from tkinter import filedialog, Tk # Tk was import tkinter as tk
from datetime import date, datetime #was import datetime
from PySide2.QtCore import *
from PySide2.QtGui import *
from PySide2.QtWidgets import *
from dateutil.relativedelta import relativedelta
from win10toast import ToastNotifier

class Cal_Item:
    def __init__(self, sn):
        self.sn = sn
        self.location = ''
        self.sublocation = ''
        self.interval = 12
        self.cal_vendor = ''
        self.manufacturer = ''
        self.lastcal = ''
        self.nextcal = ''
        self.calasneeded = False
        self.direc = ''
        self.code = ''
        self.description = ''
        self.inservice = True
        self.inservicedate = ''
        self.outofservicedate = ''
        self.caldue = False
        self.ooc = ''
        self.model = '' 


firstrun = True
dbDir = ''
newdbDir = ''
#Connect to or create the database file
def connect(override = ''):
    global conn, c, firstrun, dbDir
    if firstrun == True:
        if path.isfile('\\\\artemis\\Hardware Development Projects\\Manufacturing Engineering\\Test Equipment\\calibrations.db'):
            directory = '\\\\artemis\\Hardware Development Projects\\Manufacturing Engineering\\Test Equipment\\calibrations.db'
        else:
            directory = 'calibrations.db'
        firstrun = False
    elif dbDir != '':
        directory = dbDir
    else:
        directory = '\\\\artemis\\Hardware Development Projects\\Manufacturing Engineering\\Test Equipment\\calibrations.db'
        dbDir = directory
    if override != '':
        directory = override
    conn = sqlite3.connect(directory)
    c = conn.cursor()
    #print('Connected to db: {0}'.format(directory))
    return conn, c

#Save changes and close connection
def disconnect(save = True):
    if save == True:
        conn.commit()
    else:
        pass
    conn.close()

def allItems():
    all_items = c.execute("SELECT * FROM calibration_items")
    return all_items

#Create the default tables for the file
def create_tables(override = ''):
    if override != '':
        connect(override)
    else:
        connect()
    c.execute("""CREATE TABLE IF NOT EXISTS calibration_items (
                    serial_number TEXT PRIMARY KEY,
                    location TEXT DEFAULT '',
                    interval INTEGER DEFAULT 12,
                    cal_vendor TEXT DEFAULT '',
                    manufacturer TEXT DEFAULT '',
                    lastcal DEFAULT '',
                    nextcal DEFAULT '',
                    calasneeded INTEGER DEFAULT 0,
                    directory TEXT DEFAULT '',
                    description TEXT DEFAULT '',
                    inservice INTEGER DEFAULT 1,
                    inservicedate DEFAULT '',
                    outofservicedate DEFAULT '',
                    caldue INTEGER DEFAULT 0,
                    model TEXT DEFAULT '',
                    comment TEXT DEFAULT ''
                    )""")
    c.execute("""CREATE TABLE IF NOT EXISTS directories (
                    id INTEGER PRIMARY KEY NOT NULL,
                    calListDir TEXT DEFAULT '\\\\artemis\\Hardware Development Projects\\Manufacturing Engineering\\Test Equipment',
                    tempFilesDir TEXT DEFAULT 'C:\\Users\\grobinson\\Documents\\Calibrations',
                    calScansDir TEXT DEFAULT '\\\\artemis\\Hardware Development Projects\\Manufacturing Engineering\\Test Equipment\\Calibration Scans'
                    )""")
    c.execute("""INSERT OR IGNORE INTO directories (id)
                 VALUES (?)""",(1,))
    
    c.execute("""PRAGMA user_version""")
    version = c.fetchone()[0]
    if str(version) == '0':
        c.execute("""ALTER TABLE directories ADD dbDir TEXT DEFAULT '\\\\artemis\\Hardware Development Projects\\Manufacturing Engineering\\Test Equipment\\calibrations.db'""")
        c.execute("""ALTER TABLE calibration_items ADD timestamp TEXT DEFAULT ''""")
        c.execute("""PRAGMA user_version = 1""")
        c.execute("""PRAGMA user_version""")
        version = c.fetchone()[0]
    disconnect()
#Transfer the data from previous pickle file to the new database
@Slot()
def migrate():
    warningMessage = QMessageBox()
    warningMessage.setWindowTitle('Data Migration')
    warningMessage.setText('Migrate data from a .pkl file? Any overlapping data will be overwritten.')
    warningMessage.addButton(QMessageBox.Ok)
    cancelButton = warningMessage.addButton(QMessageBox.Cancel)
    warningMessage.exec_()
    if warningMessage.clickedButton() == cancelButton:
        return
    else:
        pass
    connect()
    try:
        with open('calitemslist.pkl','rb') as cals:
            items = pickle.load(cals)
    except Exception as e:
        message = QMessageBox()
        message.setWindowTitle('Error')
        message.setText('{}'.format(e))
        message.exec_()

    for item in items:
        if item.calasneeded == True:
            current_calasneeded = 1
        else:
            current_calasneeded = 0
        if item.inservice == True:
            current_inservice = 1
        else:
            current_inservice = 0
        if item.caldue == True:
            current_caldue = 1
        else:
            current_caldue = 0
        if item.sublocation != '':
            current_location = '{}-{}'.format(item.location,item.sublocation)
        else:
            current_location = item.location
        c.execute("""INSERT OR REPLACE INTO calibration_items (serial_number, location, interval, cal_vendor, manufacturer, lastcal, nextcal, calasneeded, 
                    directory, description, inservice, inservicedate, outofservicedate, caldue, model)
                    VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)""", 
                    (item.sn, current_location, item.interval, item.cal_vendor, item.manufacturer, str(item.lastcal), str(item.nextcal),
                            current_calasneeded, item.direc, item.description, current_inservice, str(item.inservicedate), str(item.outofservicedate),
                            current_caldue, item.model,))
    conn.commit()
    conn.close()

    message = QMessageBox()
    message.setWindowTitle('Done')
    message.setText('Data migration complete')
    message.exec_()


#Load latest data from DB
def load():
    global calListDir, tempFilesDir, calScansDir, dbDir, firstrun
    connect()
    #Directory Data
    c.execute("SELECT * FROM directories WHERE id = 1")
    directories = c.fetchone()
    calListDir = directories[1]
    tempFilesDir = directories[2]
    calScansDir = directories[3]
    dbDir = directories[4]
    disconnect()

#Change set directories
def changedir(choice):
    global calListDir, tempFilesDir, calScansDir, dbDir
    root = Tk()
    root.withdraw()
    direc = filedialog.askdirectory()
    directory = direc.replace('/', '\\')
    if directory != '':
        return directory
    root.destroy()

#Opens the Cal-PM List
def calList():
    file = calListDir + '\\Test Equipment Cal-PM List.xlsx'
    startfile(file)

#Creates a new, blank, Excel report for today's date
def newReport(sn):
    updateitems()
    connect()
    all_items = allItems()
    for item in all_items:
        if item[0] == sn:
            direc = item[8]
            todaystr = datetime.strftime(date.today(),'%Y-%m-%d')
            today = datetime.strptime(todaystr,'%Y-%m-%d').date()
            if path.isdir(direc):
                if path.isfile('{0}/{1}_{2}.xlsx'.format(direc,today,item[0])):
                    startfile('{0}/{1}_{2}.xlsx'.format(direc,today,item[0]))
                else:
                    newfile = '{0}/{1}_{2}.xlsx'.format(direc,today,item[0])
                    copyfile(tempFilesDir + '/' + 'ReportTemplate.xlsx',newfile)

                    #Set up openpyxl to use the new report spreadsheet
                    newreport = openpyxl.load_workbook(newfile)
                    ws = newreport.active
                    testercaldue = ''
                    #Fill in the info
                    ws['C5'] = item[4]
                    ws['C6'] = item[14]
                    ws['C7'] = item[0]
                    ws['C8'] = item[9]
                    ws['D12'] = 'X'
                    ws['D15'] = 'X'
                    ws['G12'] = 'X'
                    ws['G15'] = 'X'
                    ws['J12'] = 'X'
                    ws['D18'] = today
                    if item[10] == 1:
                        ws['D19'] = today
                    else:
                        pass
                    ws['D20'] = str(today + relativedelta(months = item[2]))
                    ws['I19'] = item[1]
                    for testitem in all_items:
                        if testitem[14] == 'ST102':
                            testercaldue = testitem[6]
                            break
                    ws['J29'] = testercaldue
                    ws['J42'] = today
                    newreport.save('{0}/{1}_{2}.xlsx'.format(direc,today,item[0]))
                    #Open the file so edits/signatures can be made.
                    startfile('{0}/{1}_{2}.xlsx'.format(direc,today,item[0]))
    disconnect()
    updateitems()
#Generates an Excel spreadsheet with info on every item that is out of cal (OOC) or is within 30 days of its next cal date
def report_OOC():
    items_to_add = []
    connect()
    all_items = allItems()
    for item in all_items:
        #if (item.nextcal - datetime.today().date()).days <= 30:
        if item[13] == 1:
            if item[1] == 'At calibration':
                continue
            else:
                items_to_add.append(item)
    today = datetime.strptime(str(datetime.today().date()),'%Y-%m-%d').date()
    copyfile('{}\\Out of Cal Report.xlsx'.format(tempFilesDir),'{0}\\Reports\\{1}_Out of Cal Report.xlsx'.format(tempFilesDir,today))
    report = openpyxl.load_workbook('{0}\\Reports\\{1}_Out of Cal Report.xlsx'.format(tempFilesDir,today))
    name = '{0}\\Reports\\{1}_Out of Cal Report.xlsx'.format(tempFilesDir,today)
    row = 4
    ws = report.active
    #Columns A-F, SN, Description, Location, Manufacturer, Cal Vendor, Last cal, Next cal
    ws['A2'] = str(today)
    for item in items_to_add:
        if item[7] == 1:
            ws['A{}'.format(row)] = '*{}'.format(item[0])
        else:
            ws['A{}'.format(row)] = item[0]
        ws['B{}'.format(row)] = item[14]
        ws['C{}'.format(row)] = item[9]
        ws['D{}'.format(row)] = item[1]
        ws['E{}'.format(row)] = item[4]
        ws['F{}'.format(row)] = item[3]
        ws['G{}'.format(row)] = item[5]
        ws['H{}'.format(row)] = item[6]
        row += 1
    report.save(name)
    disconnect(save = False)
    try:
        startfile(name)
    except OSError:
        message = QMessageBox()
        message.setWindowTitle('Error')
        message.setText('The file could not be opened.')
        message.exec_()
#Updates calitemslist
def namereader(file='',folder='', all = False, option = None):
    #Folder will be "ENGINEERING" or "PRODUCTION" EQUIPMENT folder 
    if folder != '' and all == True:
        if 'PRODUCTION' not in folder and 'ENGINEERING' not in folder:
            return
        elif path.isdir(folder) == False:
            return
        else:
            pass
        connect()
        #Each "itemFolder" contains all files for one item
        for itemFolder in listdir(folder):
            sn = itemFolder
            if path.isdir('{}\\{}'.format(folder, itemFolder)):
                c.execute("""INSERT OR IGNORE INTO calibration_items (serial_number)
                                VALUES (?)""", (sn,))
            else:
                continue
            datesList = []
            for file in listdir('{}\\{}'.format(folder, itemFolder)):
                fileSplit = file.split('_')
                if len(fileSplit) != 2:
                    continue
                else:
                    try:
                        date = datetime.strptime(fileSplit[0],'%Y-%m-%d').date()
                        if len(fileSplit[0]) == 10:
                            datesList.append(fileSplit[0])
                    except Exception as e:
                        continue
            c.execute("SELECT * FROM calibration_items WHERE serial_number = ?", (sn,))
            itemInfo = c.fetchone()
            interval = itemInfo[2]
            if isinstance(interval, int):
                if interval > 0:
                    pass
                else:
                    interval = 12
            else:
                interval = 12
            if len(datesList) > 0:
                lastcal = max(datesList)
            else:
                lastcal = ''
                nextcal = ''
                inservicedate = ''
            inservicedate = itemInfo[11]
            if inservicedate != '' and lastcal !='':
                if inservicedate > lastcal:
                    nextcal = str(datetime.strptime(inservicedate,'%Y-%m-%d').date() + relativedelta(months = interval))
                else:
                    inservicedate = lastcal
                    nextcal = str(datetime.strptime(lastcal,'%Y-%m-%d').date() + relativedelta(months = interval))
            elif inservicedate == '' and lastcal !='':
                if itemInfo[10] == 1:
                    inservicedate = lastcal
                    nextcal = str(datetime.strptime(lastcal,'%Y-%m-%d').date() + relativedelta(months = interval))
            try:
                if (datetime.strptime(nextcal,'%Y-%m-%d').date() - datetime.today().date()).days <= 30:
                    caldue = 1
                else:
                    caldue = 0
            except Exception as e:
                caldue = 0
            if lastcal == '':
                caldue = 1
            directory = r"{}\{}".format(folder, itemFolder)
            sql = "UPDATE calibration_items SET interval={},lastcal='{}',nextcal='{}',directory='{}',inservicedate='{}',caldue={} WHERE {}=?".format(interval,lastcal,nextcal,directory,inservicedate,caldue,'serial_number')
            c.execute(sql,(sn,))

        disconnect()
#Checks all folders in cal scans directory
def updateitems():
    for dirs in listdir(calScansDir):
        current = calScansDir+'\\'+dirs
        for folder in listdir(current):
            namereader(folder = current+'\\'+folder,all = True,option = current)
    return

#GUI-----------------------------------------------------------------------------------------------------
qt_app = QApplication(sys.argv)

class MainWindow(QMainWindow):
    def __init__(self):
        #Initialize the parent class, then set title and min window size
        QMainWindow.__init__(self)
        self.setWindowTitle('CalTools v1.0.2')
        self.setMinimumSize(800,600)

        #Icon
        self.icon = QIcon('CalToolsIcon.png')
        self.setWindowIcon(self.icon)

        #Set QWidget as central for the QMainWindow, QWidget will hold layouts
        self.centralwidget = QWidget()
        self.setCentralWidget(self.centralwidget)

        #Create the layouts
        self.layout = QVBoxLayout()
        self.toprow = QHBoxLayout()
        self.focus_layout = QGridLayout()
        #self.form_layout = QFormLayout()
        self.bottomrow = QGridLayout()
        #Menu Bar
        self.menubar = self.menuBar()

        exitaction = QAction("Exit",self)
        migration = QAction("Migrate Data", self)
        migration.triggered.connect(migrate)
        filemenu = self.menubar.addMenu("&File")
        filemenu.addAction(exitaction)
        toolsmenu = self.menubar.addMenu("&Tools")
        toolsmenu.addAction(migration)
        #Create the top row buttons to go into the sublayout (which is then in the main layout)
        self.btn0 = QPushButton('Open Cal List')
        self.btn0.clicked.connect(self.calListClick)
        self.btn2 = QPushButton('Out of Cal Report')
        self.btn2.clicked.connect(self.calReportClick)
        self.btn3 = QPushButton('Update Items')
        self.btn3.clicked.connect(self.updateClick)
        self.btn4 = QPushButton('Calibrations Folder')
        self.btn4.clicked.connect(self.calFolderClick)
        self.btn5 = QPushButton('Settings')
        self.btn5.clicked.connect(self.settingsClick)

        #Add the buttons to the sublayout
        self.toprow.addWidget(self.btn0)
        self.toprow.addWidget(self.btn2)
        self.toprow.addWidget(self.btn3)
        self.toprow.addWidget(self.btn4)
        self.toprow.addWidget(self.btn5)

        #QTreeWidget and QFormLayout to be added to focus_layout
        self.itemslist = QTreeWidget()
        self.itemslist.setHeaderLabel('Calibration Items')
        self.itemslist.itemSelectionChanged.connect(self.showDetails)
        self.topitems = []
        self.topitems.append(QTreeWidgetItem(self.itemslist))
        self.topitems[0].setText(0,'Production Equipment')
        self.topitems.append(QTreeWidgetItem(self.itemslist))
        self.topitems[1].setText(0,'Engineering Equipment')
        self.topitems.append(QTreeWidgetItem(self.itemslist))
        self.topitems[2].setText(0,'Other Items')
        self.itemslist.addTopLevelItems(self.topitems)
        self.details = QFormLayout()

        #Items in the details form
        self.snEdit = QLineEdit(self)
        self.snEdit.setReadOnly(True)
        self.modelEdit = QLineEdit(self)
        self.modelEdit.setReadOnly(True)
        self.descEdit = QLineEdit(self)
        self.descEdit.setReadOnly(True)
        self.locationEdit = QComboBox(self)
        self.locationEdit.setEditable(True)
        #Items will be added in showDetails() method.
        self.locationEdit.setEnabled(False)
        self.manufacturerEdit = QComboBox(self)
        self.manufacturerEdit.setEnabled(False)
        self.manufacturerEdit.setEditable(True)
        self.vendorEdit = QComboBox(self)
        self.vendorEdit.setEnabled(False)
        self.vendorEdit.setEditable(True)
        self.intervalEdit = QSpinBox(self)
        self.intervalEdit.setEnabled(False)
        self.lastEdit = QLineEdit(self)
        self.lastEdit.setReadOnly(True)
        self.nextEdit = QLineEdit(self)
        self.nextEdit.setReadOnly(True)
        self.asNeededEdit = QCheckBox(self)
        self.asNeededEdit.setEnabled(False)
        self.inServiceEdit = QCheckBox(self)
        self.inServiceEdit.setEnabled(False)
        self.inServiceDateEdit = QLineEdit(self)
        self.inServiceDateEdit.setReadOnly(True)
        self.commentEdit = QTextEdit(self)
        self.commentEdit.setReadOnly(True)

        self.details.addRow('SN:',self.snEdit)
        self.details.addRow('Model: ',self.modelEdit)
        self.details.addRow('Description:',self.descEdit)
        self.details.addRow('Location:',self.locationEdit)
        self.details.addRow('Manufacturer:',self.manufacturerEdit)
        self.details.addRow('Cal vendor:',self.vendorEdit)
        self.details.addRow('Cal interval:',self.intervalEdit)
        self.details.addRow('Last calibration:',self.lastEdit)
        self.details.addRow('In Service Date (YYYY-MM-DD): ',self.inServiceDateEdit)
        self.details.addRow('Next calibration:',self.nextEdit)
        self.details.addRow('Cal as needed:',self.asNeededEdit)
        self.details.addRow('In Service: ',self.inServiceEdit)
        self.details.addRow('Comments: ',self.commentEdit)
        #---------------------------
        self.searchoptions = QComboBox(self)
        self.searchoptionsdict = {'Serial Number':0,'Model':14,'Description':9,'Location':1,'Manufacturer':4,'Cal Vendor':3,'Out of Cal':13, 'Has Comment':15}
        self.searchoptions.addItems(sorted(list(self.searchoptionsdict.keys())))
        self.searchoptions.setCurrentIndex(7)
        self.searchoptions.currentIndexChanged.connect(self.indexCheck)

        #---------------------------

        self.focus_layout.addWidget(self.itemslist,0,0)
        self.focus_layout.addWidget(self.searchoptions,1,0)
        self.focus_layout.addLayout(self.details,0,1)
        self.focus_layout.setColumnStretch(1,1)

        #Layout for bottom row buttons
        self.editItemBtn = QPushButton('Edit Item')
        self.openfolderbtn = QPushButton('Open Folder')
        self.newreportbtn = QPushButton('New Report')
        self.removebtn = QPushButton('Remove Item')
        self.findentry = QLineEdit(self)

        self.editItemBtn.clicked.connect(self.editClick)
        self.openfolderbtn.clicked.connect(self.openFolderClick)
        self.newreportbtn.clicked.connect(self.newReportClick)
        self.removebtn.clicked.connect(self.removeItemClick)
        self.findentry.textChanged.connect(self.find)
        self.findentry.setPlaceholderText('Search')

        self.bottomrow.addWidget(self.findentry,0,0,)
        self.bottomrow.setColumnMinimumWidth(0,256)
        self.bottomrow.addWidget(self.editItemBtn,0,1)
        self.bottomrow.setColumnStretch(1,1)
        self.bottomrow.addWidget(self.openfolderbtn,0,2)
        self.bottomrow.setColumnStretch(2,1)
        self.bottomrow.addWidget(self.newreportbtn,0,3)
        self.bottomrow.setColumnStretch(3,1)
        #Hidden until functionality properly implemented
        #self.bottomrow.addWidget(self.removebtn,0,4)
        self.bottomrow.setColumnStretch(4,1)
        #Add the sublayouts to the main layout
        self.layout.addLayout(self.toprow)
        self.layout.addLayout(self.focus_layout)
        self.layout.addLayout(self.bottomrow)

        self.centralwidget.setLayout(self.layout)
        self.editable = False
    def settingsWindow(self):
        self.settings = QWidget()
        self.settings.setMinimumSize(600,300)
        self.settings.setWindowTitle('CalTools Settings')
        self.settings.layout = QVBoxLayout()
        self.settings.form_layout = QFormLayout()
        self.settings.bottomButtons = QHBoxLayout()
        #----Blank label to add space between items
        self.settings.blankSpace = QLabel('')
        #----Buttons and text boxes for settings window
        self.settings.calDirecEdit = QLineEdit(self)
        self.settings.calDirecBtn = QPushButton('Browse')
        self.settings.calDirecBtn.clicked.connect(self.calDirecClick)
        self.settings.tempDirecEdit = QLineEdit(self)
        self.settings.tempDirecBtn = QPushButton('Browse')
        self.settings.tempDirecBtn.clicked.connect(self.tempDirecClick)
        self.settings.scansDirecEdit = QLineEdit(self)
        self.settings.scansDirecBtn = QPushButton('Browse')
        self.settings.scansDirecBtn.clicked.connect(self.scansDirecClick)
        self.settings.dbDirEdit = QLineEdit(self)
        self.settings.dbDirBtn = QPushButton('Browse')
        self.settings.dbDirBtn.clicked.connect(self.dbDirClick)

        self.settings.okBtn = QPushButton('OK')
        self.settings.okBtn.clicked.connect(self.settingsOk)
        self.settings.cancelBtn = QPushButton('Cancel')
        self.settings.cancelBtn.clicked.connect(self.settingsCancel)

        self.settings.form_layout.addRow('Cal-PM List Directory',self.settings.calDirecEdit)
        self.settings.form_layout.addRow('',self.settings.calDirecBtn)
        self.settings.form_layout.addRow('',self.settings.blankSpace)

        self.settings.form_layout.addRow('Template Files Directory',self.settings.tempDirecEdit)
        self.settings.form_layout.addRow('',self.settings.tempDirecBtn)
        self.settings.form_layout.addRow('',self.settings.blankSpace)

        self.settings.form_layout.addRow('Calibration Scans Directory',self.settings.scansDirecEdit)
        self.settings.form_layout.addRow('',self.settings.scansDirecBtn)
        self.settings.form_layout.addRow('',self.settings.blankSpace)

        self.settings.form_layout.addRow('Database Directory',self.settings.dbDirEdit)
        self.settings.form_layout.addRow('',self.settings.dbDirBtn)

        self.settings.layout.addLayout(self.settings.form_layout)
        self.settings.layout.addStretch(1)
        self.settings.bottomButtons.addWidget(self.settings.okBtn)
        self.settings.bottomButtons.addWidget(self.settings.cancelBtn)
        self.settings.layout.addLayout(self.settings.bottomButtons)
        self.settings.setLayout(self.settings.layout)
    def removeBox(self):
        self.remove = QWidget()
        self.remove.setFixedSize(350,100)
        self.remove.setWindowTitle('Remove Item')
        self.remove.layout = QVBoxLayout()
        self.remove.bottomButtons = QHBoxLayout()

        self.remove.mainText = QLabel('')

        self.remove.removeBtn = QPushButton('Remove')
        self.remove.removeBtn.clicked.connect(self.removeClick)
        self.remove.referenceBtn = QPushButton('Reference')
        self.remove.referenceBtn.clicked.connect(self.referenceClick)
        self.remove.deleteBtn = QPushButton('Delete')
        self.remove.deleteBtn.clicked.connect(self.deleteClick)
        self.remove.cancelBtn = QPushButton('Cancel')
        self.remove.cancelBtn.clicked.connect(self.removeCancel)

        self.remove.bottomButtons.addWidget(self.remove.cancelBtn)
        self.remove.bottomButtons.addWidget(self.remove.removeBtn)
        self.remove.bottomButtons.addWidget(self.remove.referenceBtn)
        self.remove.bottomButtons.addWidget(self.remove.deleteBtn)
        self.remove.layout.addWidget(self.remove.mainText)
        self.remove.layout.addLayout(self.remove.bottomButtons)
        self.remove.setLayout(self.remove.layout)

    #Slots for buttons---------------------
    @Slot()
    def calListClick(self):
        calList()
    @Slot()
    def calReportClick(self):
        try:
            report_OOC()
        except PermissionError:
            pass
    @Slot()
    def updateClick(self):
        updateitems()
        self.updateTree()
    @Slot()
    def calFolderClick(self):
        startfile(calScansDir)
    @Slot()
    def settingsClick(self):
        #Display current set directories
        self.settings.dbDirChanged = False
        self.settings.calDirecEdit.setText(calListDir)
        self.settings.tempDirecEdit.setText(tempFilesDir)
        self.settings.scansDirecEdit.setText(calScansDir)
        self.settings.dbDirEdit.setText(dbDir)

        self.settings.setWindowModality(Qt.ApplicationModal)
        self.settings.show()
    @Slot()
    def removeItemClick(self):
        if self.itemslist.currentItem():
            if self.itemslist.currentItem().text(0) != None and self.itemslist.currentItem().text(0) != '':
                self.remove.mainText.setText('Item {}: \nRemove from service, reference only, or delete?'.format(self.itemslist.currentItem().text(0)))
                self.remove.mainText.setAlignment(Qt.AlignHCenter)
                self.remove.setWindowModality(Qt.ApplicationModal)
                self.remove.show()
                self.remove.raise_()

    #Settings button slots
    @Slot()
    def calDirecClick(self):
        global calListDir
        directory = changedir(1)
        if directory != None:
            calListDir = directory
            self.settings.calDirecEdit.setText(directory)
    @Slot()
    def tempDirecClick(self):
        global tempFilesDir
        directory = changedir(2)
        if directory != None:
            tempFilesDir = directory     
            self.settings.tempDirecEdit.setText(directory)
    @Slot()
    def scansDirecClick(self):
        global calScansDir
        directory = changedir(3)
        if directory != None:
            calScansDir = directory
            self.settings.scansDirecEdit.setText(directory)

    @Slot()
    def dbDirClick(self):
        global dbDir, newdbDir
        directory = changedir(4)
        if directory != None:
            self.settings.dbDirEdit.setText(directory)
            self.settings.dbDirChanged = True
            newdbDir = directory

    @Slot()
    def settingsCancel(self):
        global newdbDir
        if self.settings.dbDirChanged == True:
            self.settings.dbDirChanged = False
            newdbDir = ''
        load()
        print(dbDir)
        self.settings.hide()
    @Slot()
    def settingsOk(self):
        global newdbDir,dbDir
        if self.settings.dbDirChanged == True:
            try:
                move(dbDir,newdbDir)
            except:
                pass
            dbDir = newdbDir + '\\calibrations.db'
        connect()
        c.execute("UPDATE directories SET calListDir = ? WHERE id = 1",(calListDir,))
        c.execute("UPDATE directories SET tempFilesDir = ? WHERE id = 1",(tempFilesDir,))
        c.execute("UPDATE directories SET calScansDir = ? WHERE id = 1",(calScansDir,))
        if self.settings.dbDirChanged == True:
            c.execute("UPDATE directories SET dbDir = ? WHERE id = 1",(dbDir,))
        disconnect()
        newdbDir = ''
        load()
        #print(dbDir)
        self.settings.hide()

    #Item Removal Button Slots
    @Slot()
    def removeClick(self):
        for item in calitemslist:
            if self.itemslist.currentItem().text(0) == item.sn:
                try:
                    move(item.direc,'{}\\Removed from Service'.format(calScansDir))
                except FileNotFoundError:
                    pass
                calitemslist.remove(item)
                self.updateTree()
                save()
                self.remove.hide()
    @Slot()
    def referenceClick(self):
        for item in calitemslist:
            if self.itemslist.currentItem().text(0) == item.sn:
                try:
                    move(item.direc,'{}\\Ref Only'.format(calScansDir))
                except FileNotFoundError:
                    pass
                calitemslist.remove(item)
                self.updateTree()
                save()
                self.remove.hide()   
    @Slot()
    def deleteClick(self):
        for item in calitemslist:
            if self.itemslist.currentItem().text(0) == item.sn:
                #try:
                #    rmtree(item.direc)
                #except FileNotFoundError:
                #    pass
                calitemslist.remove(item)
                self.updateTree()
                save()
                self.remove.hide()
    @Slot()
    def removeCancel(self):
        self.remove.hide()

    #Slots for item details
    @Slot()
    def showDetails(self):
        if self.editable == True:
            self.editClick(toggle = True)
        try:
            connect()
            all_items = allItems()
            #Get all locations currently in use and add to the QComboBox
            locationsList = []
            manufacturersList = []
            vendorsList = []
            for item in all_items:
                if item[1] not in locationsList and item[1] != '':
                    locationsList.append(item[1])
                if item[4] not in manufacturersList and item[4] != '':
                    manufacturersList.append(item[4])
                if item[3] not in vendorsList and item[3] != '':
                    vendorsList.append(item[3])
            all_items = allItems()
            for item in all_items:
                if self.itemslist.currentItem().text(0) == item[0]:
                    #Clear previous values in the combo boxes
                    self.locationEdit.clear()
                    self.manufacturerEdit.clear()
                    self.vendorEdit.clear()

                    self.snEdit.setPlaceholderText(item[0])
                    self.modelEdit.setPlaceholderText(item[14])
                    self.descEdit.setPlaceholderText(item[9])
  
                    self.locationEdit.addItems(sorted(locationsList))
                    self.locationEdit.setCurrentText(item[1])
                    self.manufacturerEdit.addItems(sorted(manufacturersList))
                    self.manufacturerEdit.setCurrentText(item[4])
                    self.vendorEdit.addItems(sorted(vendorsList))
                    self.vendorEdit.setCurrentText(item[3])

                    self.intervalEdit.setValue(item[2])
                    self.lastEdit.setPlaceholderText(item[5])
                    self.nextEdit.setPlaceholderText(item[6])
                    self.inServiceDateEdit.setText(item[11])
                    self.commentEdit.setPlaceholderText(item[15])
                    if item[7] == 0:
                        self.asNeededEdit.setChecked(False)
                    else:
                        self.asNeededEdit.setChecked(True)
                    if item[10] == 0:
                        self.inServiceEdit.setChecked(False)
                    else:
                        self.inServiceEdit.setChecked(True)
                    self.asNeededEdit.setEnabled(False)
                    self.inServiceEdit.setEnabled(False)
                    #self.editClick(toggle = True)
            disconnect(save = False)
        except Exception as e:
            print('showDetails method: '+ str(e))
            

    #Bottom row button slots
    @Slot()
    def indexCheck(self):
        if self.searchoptions.currentIndex() == 6 or self.searchoptions.currentIndex() == 2:
            self.findentry.clear()
            self.findentry.setReadOnly(True)
            self.find()
        else:
            self.findentry.setReadOnly(False)
            self.find()
            return
    @Slot()
    def find(self):
        self.topitems[0].setExpanded(True)
        self.topitems[1].setExpanded(True)

        #Calls the update method to search for items when text is changed
        searchmode = self.searchoptionsdict[self.searchoptions.currentText()]
        text = self.findentry.text()
        self.updateTree(search = True, text = text, mode = searchmode)
    @Slot()
    def openFolderClick(self):
        try:
            updateitems()
            connect()
            all_items = allItems()
            for item in all_items:
                if self.itemslist.currentItem().text(0) == item[0]:
                    startfile(item[8])
                    break
            disconnect(save = False)
        except FileNotFoundError:
            message = QMessageBox()
            message.setWindowTitle('File Not Found')
            message.setText('Cannot find the folder of the selected item. Check directory in settings.')
            message.exec_()

        except AttributeError as e:
            print('OpenFolderClick method: ' + e)
    @Slot()
    def newReportClick(self):
        sn = self.itemslist.currentItem().text(0)
        newReport(sn)
    @Slot()
    def editClick(self,toggle = False):
        if len(self.itemslist.selectedItems()) == 0:
            return
        if (self.snEdit.isReadOnly() and toggle == False) or (toggle == True and self.editable == False):
            self.editItemBtn.setText('Save')
            self.snEdit.setReadOnly(False)
            self.snEdit.setText(self.snEdit.placeholderText())
            self.modelEdit.setReadOnly(False)
            self.modelEdit.setText(self.modelEdit.placeholderText())
            self.descEdit.setReadOnly(False)
            self.descEdit.setText(self.descEdit.placeholderText())
            self.locationEdit.setEnabled(True)
            self.manufacturerEdit.setEnabled(True)
            self.vendorEdit.setEnabled(True)
            self.intervalEdit.setEnabled(True)
            #self.lastEdit.setReadOnly(False)
            #self.nextEdit.setReadOnly(False)
            self.asNeededEdit.setEnabled(True)
            self.inServiceEdit.setEnabled(True)
            self.inServiceDateEdit.setReadOnly(False)
            self.commentEdit.setText(self.commentEdit.placeholderText())
            self.commentEdit.setReadOnly(False)
            self.editable = True
            return
        elif (self.snEdit.isReadOnly() == False and toggle == False) or (toggle == True and self.editable == True):
            if toggle == False:
                connect()
                all_items = allItems()
                for item in all_items:
                    if self.itemslist.currentItem().text(0) == item[0]:
                        if len(self.snEdit.text()) != 0:
                            sn = self.snEdit.text()
                        model = self.modelEdit.text()
                        description = self.descEdit.text()
                        location = self.locationEdit.currentText()
                        manufacturer = self.manufacturerEdit.currentText()
                        cal_vendor = self.vendorEdit.currentText()
                        interval = self.intervalEdit.value()

                        #Selects the text from the QTextEdit object so that it can be saved. QTextEdit has no .text() method
                        self.commentEdit.selectAll()
                        text = self.commentEdit.textCursor().selectedText()

                        comment = text
                        if self.asNeededEdit.isChecked():
                            calasneeded = 1
                        else:
                            calasneeded = 0
                        if self.inServiceEdit.isChecked() == True:
                            inservice = 1
                            try:
                                if self.inServiceDateEdit.text() != '':
                                    inservicedate = str(datetime.strptime(self.inServiceDateEdit.text(),'%Y-%m-%d').date())
                                elif self.inServiceDateEdit.text() == '' and item[5] == '':
                                    inservicedate = ''
                                else:
                                    inservicedate = item[5]
                                if inservicedate < item[5]:
                                    inservicedate = item[5]
                            except:
                                message = QMessageBox()
                                message.setWindowTitle('Format Error')
                                message.setText('The format of the "In Service Date" is incorrect. The date was not changed, please re-enter.')
                                message.exec_()
                                return
                            if inservicedate != '':
                                nextcal = str(datetime.strptime(inservicedate,'%Y-%m-%d').date() + relativedelta(months =+ item[2]))
                            else:
                                nextcal = ''
                            outofservicedate = ''
                        elif item[10] == 1 and self.inServiceEdit.isChecked() == False:
                            inservice = 0
                            outofservicedate = datetime.strftime(datetime.today().date(),'%Y-%m-%d')
                            inservicedate = ''
                            nextcal = ''
                        selecteditem = self.itemslist.currentItem().text(0)
                        timestamp = datetime.utcnow().strftime('%Y-%m-%d-%H-%M-%S-%f')
                        sql = """
                                UPDATE calibration_items SET serial_number='{}',model='{}',description='{}',location='{}',manufacturer='{}',cal_vendor='{}',interval={},calasneeded={},inservice={},inservicedate='{}',
                                nextcal='{}',outofservicedate='{}',comment='{}',timestamp='{}' WHERE serial_number=?
                                """.format(sn,model,description,location,manufacturer,cal_vendor,interval,calasneeded,inservice,inservicedate,nextcal,outofservicedate,comment,timestamp)
                        c.execute(sql,(sn,))
                        disconnect()
                        if self.findentry.text() != '' or self.searchoptions.currentIndex() == 6 or self.searchoptions.currentIndex() == 2:
                            self.find()
                        else:
                            self.updateTree()
                        #Arbitrary number for containing the loops
                        for number in [1]:
                            for item in self.productionItems:
                                if item.text(0) == selecteditem:
                                    self.itemslist.setCurrentItem(item)
                                    break
                            for item in self.engineeringItems:
                                if item.text(0) == selecteditem:
                                    self.itemslist.setCurrentItem(item)
                                    break
                            for item in self.otherItems:
                                if item.text(0) == selecteditem:
                                    self.itemslist.setCurrentItem(item)
                                    break
                        break
            self.editItemBtn.setText('Edit Item')
            self.locationEdit.setEnabled(False)
            self.manufacturerEdit.setEnabled(False)
            self.vendorEdit.setEnabled(False)
            self.intervalEdit.setEnabled(False)
            #self.lastEdit.setReadOnly(True)
            #self.nextEdit.setReadOnly(True)
            self.inServiceEdit.setEnabled(False)
            self.inServiceDateEdit.setReadOnly(True)
            self.commentEdit.setReadOnly(True)
            self.asNeededEdit.setEnabled(False) 
            self.editable = False
            #Handles updating of placeholder text
            #if toggle == False:
                #self.snEdit.setPlaceholderText(self.snEdit.text())
                #self.modelEdit.setPlaceholderText(self.modelEdit.text())
                #self.descEdit.setPlaceholderText(self.descEdit.text())
            if len(self.snEdit.placeholderText()) == 0:
                self.snEdit.setPlaceholderText(self.itemslist.currentItem().text(0))
            self.snEdit.clear()
            self.modelEdit.clear()
            self.descEdit.clear()
            self.commentEdit.clear()
            self.snEdit.setReadOnly(True)
            self.descEdit.setReadOnly(True)
            return
    #--------------------------------------
    def updateTree(self,search = False, text = None, mode = 0):
        if self.findentry.text() != '':
            search = True
            text = self.findentry.text()
            connect()
            searchItems = listSearch.search(text, allItems(),mode)
            disconnect()
        elif mode == 15 or mode == 13:
            searchItems = ()
            pass
        else:
            search = False
        self.topitems[0].takeChildren()
        self.topitems[1].takeChildren()
        self.topitems[2].takeChildren()
        self.productionItems = []
        self.engineeringItems = []
        self.otherItems = []
        connect()
        all_items = allItems()
        for item in all_items:
            if item[8] != '':
                if 'ENGINEERING EQUIPMENT' in item[8]:
                    if search == False:
                        self.engineeringItems.append(QTreeWidgetItem(self.topitems[1]))
                        self.engineeringItems[-1].setText(0, item[0])
                    else:
                        if item in searchItems and mode != 13 and mode != 15:
                            self.engineeringItems.append(QTreeWidgetItem(self.topitems[1]))
                            self.engineeringItems[-1].setText(0, item[0])
                        elif mode == 13:
                            if item[13] != 0:
                                self.engineeringItems.append(QTreeWidgetItem(self.topitems[1]))
                                self.engineeringItems[-1].setText(0, item[0])
                        elif mode == 15:
                            if item[15] != '':
                                self.engineeringItems.append(QTreeWidgetItem(self.topitems[1]))
                                self.engineeringItems[-1].setText(0, item[0])
                elif 'PRODUCTION EQUIPMENT' in item[8]:
                    if search == False:
                        self.productionItems.append(QTreeWidgetItem(self.topitems[0]))
                        self.productionItems[-1].setText(0, item[0])
                    else:
                        if item in searchItems and mode != 13 and mode != 15:
                            self.productionItems.append(QTreeWidgetItem(self.topitems[0]))
                            self.productionItems[-1].setText(0, item[0])
                        elif mode == 13:
                            if item[13] != 0:
                                self.productionItems.append(QTreeWidgetItem(self.topitems[0]))
                                self.productionItems[-1].setText(0, item[0])
                        elif mode == 15:
                            if item[15] != '':
                                self.productionItems.append(QTreeWidgetItem(self.topitems[0]))
                                self.productionItems[-1].setText(0, item[0])
            else:
                if search == False:
                    self.otherItems.append(QTreeWidgetItem(self.topitems[2]))
                    self.otherItems[-1].setText(0, item[0])
                else:
                    if item in searchItems and mode != 13 and mode != 15:
                        self.otherItems.append(QTreeWidgetItem(self.topitems[2]))
                        self.otherItems[-1].setText(0, item[0])
                    elif mode == 13:
                        if item[13] != 0:
                            self.otherItems.append(QTreeWidgetItem(self.topitems[2]))
                            self.otherItems[-1].setText(0, item[0])
                    elif mode == 15:
                        if item[15] != '':
                                self.otherItems.append(QTreeWidgetItem(self.topitems[2]))
                                self.otherItems[-1].setText(0, item[0])
        disconnect(save = False)

        self.topitems[0].addChildren(self.productionItems)
        self.topitems[0].sortChildren(0,Qt.AscendingOrder)
        self.topitems[1].addChildren(self.engineeringItems)
        self.topitems[1].sortChildren(0,Qt.AscendingOrder)
        self.topitems[2].addChildren(self.otherItems)
        self.topitems[2].sortChildren(0,Qt.AscendingOrder)
        return


    def run(self):
        connect()
        disconnect()
        create_tables()
        load()
        updateitems()
        self.settingsWindow()
        self.removeBox()
        self.updateTree()
        self.show()
        qt_app.exec_()

if __name__ == '__main__':
    app = MainWindow()
    app.run()