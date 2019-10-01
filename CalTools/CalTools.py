import pickle, sys, openpyxl, sqlite3, listSearch, time, json
from os import remove, path, startfile, listdir, unlink, mkdir
from shutil import copyfile, move, rmtree
from tkinter import filedialog, Tk
from datetime import date, datetime
from PySide2.QtCore import *
from PySide2.QtGui import *
from PySide2.QtWidgets import *
from dateutil.relativedelta import relativedelta
from win10toast import ToastNotifier
from importlib import reload
from importlib.machinery import SourceFileLoader
try:
    config = SourceFileLoader('config','./config.py').load_module()
except FileNotFoundError:
    with open('config.py','wt',encoding = "UTF-8") as cfg:
        lines = [
                    "\n",
                    "folders  = [\"PRODUCTION EQUIPMENT\",\"ENGINEERING EQUIPMENT\",\"QUALITY EQUIPMENT\", \"Ref Only\"]\n",
                    "\n",
                    "#Prefix directory strings with r\n",
                    "\n",
                    "#Database name\n",
                    'dbName = r"Test Equipment Calibration List.db"\n',
                    '\n',
                    '#Location of Database\n',
                    'calListDir = r"\\\\artemis\Hardware Development Projects\Manufacturing Engineering\Test Equipment"\n',
                    '\n',
                    '#Location of template files\n',
                    'tempFilesDir = r"\\\\artemis\Hardware Development Projects\Manufacturing Engineering\Test Equipment\Template Files"\n',
                    '\n',
                    '#Folder that includes "Calibration Items" folder\n',
                    'calScansDir = r"\\\\artemis\Hardware Development Projects\Manufacturing Engineering\Test Equipment\Calibration Scans"\n',
                    '\n',
                    'firstRun = True'
                ]
        for line in lines:
            cfg.write(line)
    config = SourceFileLoader('config','config.py').load_module()

dbVersion = '2'

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
dbDir = config.calListDir + '\\' + config.dbName
newdbDir = ''
#Connect to or create the database file
def connect(override = ''):
    global conn, c, firstrun, dbDir
    if config.firstRun == True:
        if path.isfile(dbDir):
            directory = dbDir
        else:
            directory = config.dbName
        config.firstRun = False
    elif dbDir != '':
        directory = dbDir
    else:
        directory = '\\\\artemis\\Hardware Development Projects\\Manufacturing Engineering\\Test Equipment\\calibrations.db'
        dbDir = directory
    if override != '':
        directory = override
    try:
        conn = sqlite3.connect(directory)
        c = conn.cursor()
    except Exception as e:
        errorMessage('Error!', str(e))
        return
    return conn, c

#Save changes and close connection
def disconnect(save = True):
    if save == True:
        conn.commit()
    conn.close()

def allItems():
    allItemsC = conn.cursor()
    all_items = allItemsC.execute("SELECT * FROM calibration_items")
    return all_items

#Create the default tables for the file
def create_tables(override = ''):
    while True:
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

        c.execute("""DROP TABLE IF EXISTS directories""")
    
        c.execute("""PRAGMA user_version""")
        version = c.fetchone()[0]

        if int(version) < int(dbVersion):
            if str(version) == '0':
                c.execute("""ALTER TABLE calibration_items ADD timestamp TEXT DEFAULT ''""")
                c.execute("""PRAGMA user_version = 1""")
            elif str(version) == '1':
                c.execute("""ALTER TABLE calibration_items ADD item_group TEXT DEFAULT ''""")
                c.execute("""CREATE TABLE IF NOT EXISTS item_groups (
                                name TEXT PRIMARY KEY,
                                items TEXT DEFAULT '[]',
                                include_all_model INTEGER DEFAULT 0
                                )""")
                c.execute("""PRAGMA user_version = 2""")

        elif int(version) > int(dbVersion):
            errorMessage('Version Error','The db version for this CalTools version is {}, the latest db version is {}. Update CalTools to work with the selected database.'.format(dbVersion, version))
            sys.exit(0)
        elif int(version) == int(dbVersion):
            disconnect()
            break
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
        return
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

#Load directory data from config
def load():
    global calListDir, tempFilesDir, calScansDir, dbDir
    #Directory Data
    calListDir = config.calListDir
    tempFilesDir = config.tempFilesDir
    calScansDir = config.calScansDir
    dbDir = config.calListDir + '\\' + config.dbName

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
                    for testitem in allItems():
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
            elif 'Ref Only' in item[8]:
                continue
            else:
                
                items_to_add.append(item)
    today = datetime.strptime(str(datetime.today().date()),'%Y-%m-%d').date()
    if not path.isdir('{0}\\Calibration Items\\Snapshot Reports'.format(calScansDir)):
        mkdir('{0}\\Calibration Items\\Snapshot Reports'.format(calScansDir))
    copyfile('{}\\Out of Cal Report.xlsx'.format(tempFilesDir),'{0}\\Calibration Items\\Snapshot Reports\\{1}_Out of Cal Report.xlsx'.format(calScansDir,today))
    report = openpyxl.load_workbook('{0}\\Calibration Items\\Snapshot Reports\\{1}_Out of Cal Report.xlsx'.format(calScansDir,today))
    name = '{0}\\Calibration Items\\Snapshot Reports\\{1}_Out of Cal Report.xlsx'.format(calScansDir,today)
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
        ws['I{}'.format(row)] = item[17]
        #ws['J{}'.format(row)] = 
        ws['K{}'.format(row)] = item[15]
        row += 1
    report.save(name)
    disconnect()
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
        cont = False

        for upperFolder in config.folders:
            if upperFolder in folder:
                cont = True
        if not cont:
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
                    #Removing the use of "in service" dates, in favor of better scheduling of calibration
                    #nextcal = str(datetime.strptime(inservicedate,'%Y-%m-%d').date() + relativedelta(months = interval))
                    nextcal = str(datetime.strptime(lastcal,'%Y-%m-%d').date() + relativedelta(months = interval))
                else:
                    inservicedate = lastcal
                    nextcal = str(datetime.strptime(lastcal,'%Y-%m-%d').date() + relativedelta(months = interval))
            elif inservicedate == '' and lastcal !='':
                if itemInfo[10] == 1:
                    inservicedate = lastcal
                else:
                    inservicedate = ''
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
            if 'Ref Only' in directory:
                lastcal = ''
                nextcal = ''
                interval = 0
                caldue = 0
            sql = "UPDATE calibration_items SET interval={},lastcal='{}',nextcal='{}',directory='{}',inservicedate='{}',caldue={} WHERE {}=?".format(interval,lastcal,nextcal,directory,inservicedate,caldue,'serial_number')
            c.execute(sql,(sn,))

        disconnect()
#Checks all folders in cal scans directory
def updateitems():
    for dirs in listdir(calScansDir):
        current = calScansDir+'\\'+dirs
        for folder in listdir(current):
            if folder not in config.folders:
                continue
            namereader(folder = current+'\\'+folder,all = True,option = current)
    return
#Removes an item from the database
def removeFromDB(sn):
    c.execute("DELETE FROM calibration_items WHERE serial_number = ?",(sn,))
    removeFromGroups(sn)
def removeFromGroups(sn):
    removeFromGroupsC = conn.cursor()
    for group in allGroups():
        groupItems = json.loads(group[1])
        if sn in groupItems:
            groupItems.remove(sn)
            newItems = json.dumps(groupItems)
            removeFromGroupsC.execute("UPDATE item_groups SET items='{}' WHERE name=?".format(newItems),(group[0],))
#Functions dealing with groups-------------------------------------------------------------------------------------------------------------
def checkReplacements(item):
    if item[17] != '':
        for group in allGroups():
            if group[0] == item[17]:
                groupItems = json.loads(group[1])
                if len(groupItems) > 1:
                    groupItems.remove(item[0])
                    for sn in groupItems:
                        for snItem in allItems():
                            if snItem[0] == sn:
                                if 'Production' in snItem[1]:
                                    groupItems.remove(item[0])
                    message = 'Prepare one of the following items to replace {} by {}: {}'.format(item[0],item[6],groupItems)
                break

    else:
        message = 'No group assigned. Replace or calibrate by {}.'.format(item[6])
    return message
def allGroups():
    allGroupsC = conn.cursor()
    groups = allGroupsC.execute("SELECT * FROM item_groups")
    return groups

def addToGroup(sn,group):
    #Return an int: 0 = success, 1 = item already in group list, 2 = item or group does not exist, 3 = sql error
    selectedItem = None
    selectedGroup = None
    addToGroupC = conn.cursor()
    if group == '':
        return 0
    for item in allItems():
        if item[0] == sn:
            selectedItem = item
            break
    for g in allGroups():
        if g[0] == group:
            selectedGroup = g
            break

    if selectedItem != None and selectedGroup != None:
        if selectedGroup != selectedItem[17] and selectedItem[17] != '':
            oldGroup = None
            for g in allGroups():
                if g[0] == selectedItem[17]:
                    oldGroup = g
                    break
            oldGroupItems = json.loads(oldGroup[1])
            if selectedItem[0] in oldGroupItems:
                oldGroupItems.remove(selectedItem[0])
            addToGroupC.execute("UPDATE item_groups SET items='{}' WHERE name=?".format(json.dumps(oldGroupItems)),(oldGroup[0],))
        itemList = json.loads(selectedGroup[1])
        if selectedItem[0] not in itemList:
            itemList.append(selectedItem[0])
        else:
            return 1
    else:
        if selectedItem == None:
            errorMessage('Item not found!','Item {} does not exist in the database.'.format(sn))
            return 2
        elif selectedGroup == None and group == '':
            return 0
        elif selectedGroup == None and group != '':
            errorMessage('Group not found!','Group {} was not found, or failed to be added to the database.'.format(group))
            return 2
    try:
        addToGroupC.execute("UPDATE item_groups SET items='{}' WHERE name=?".format(json.dumps(itemList)),(selectedGroup[0],))
        addToGroupC.execute("UPDATE calibration_items SET item_group='{}' WHERE serial_number=?".format(selectedGroup[0]),(selectedItem[0],))
        return 0
    except Exception as e:
        errorMessage('Error adding item to group!', str(e))
        return 3

#GUI & Button functions-----------------------------------------------------------------------------------------------------
qt_app = QApplication(sys.argv)
centralwidget = QWidget()
calendarWidget = QWidget()

class CalendarWidget(QCalendarWidget):
    def __init__(self):
        QCalendarWidget.__init__(self)
        self.dates = self.updateCalendar()
    def paintCell(self, painter, rect, date):
            painter.setRenderHint(QPainter.Antialiasing, True)
            if (date in self.dates) and date != self.selectedDate():
                painter.save()
                painter.drawRect(rect)
                painter.setPen(Qt.blue)
                painter.drawText(QRectF(rect), Qt.TextSingleLine|Qt.AlignCenter, str(date.day()))
                painter.restore()
            else:
                QCalendarWidget.paintCell(self, painter, rect, date)
    def updateCalendar(self, index = 3, inService = False):
        #Index comes from calendarShowItems method that passes the selection of the combo box
        dates = []
        connect()
        for item in allItems():
            if index == 0:
                if 'ENGINEERING EQUIPMENT' not in item[8]:
                    continue
            elif index == 1:
                if 'PRODUCTION EQUIPMENT' not in item[8]:
                    continue
            elif index == 2:
                if 'QUALITY EQUIPMENT' not in item[8]:
                    continue
            if inService:
                if item[10] != 1:
                    continue
            date  = item[6].split('-')
            if date[0] == '':
                continue
            year = int(date[0])
            month = int(date[1])
            day = int(date[2])
            dates.append(QDate(year,month,day))
        disconnect()
        return dates
class MainWindow(QMainWindow):
    def __init__(self):
        global centralwidget, calendarWidget
        #Initialize the parent class, then set title and min window size
        QMainWindow.__init__(self)
        self.setWindowTitle('CalTools 2.1.0')
        self.setMinimumSize(1000,600)

        #Icons
        self.icon = QIcon('CalToolsIcon.png')
        self.calendarIcon = QIcon('calendar.png')
        self.toolsIcon = QIcon('CalToolsIcon.png')
        self.setWindowIcon(self.icon)

        #Set QWidget as central for the QMainWindow, QWidget will hold layouts
        #self.centralwidget = QWidget()
        self.setCentralWidget(centralwidget)

        #Calendar view
        #self.calendarWidget = QWidget()
        self.calendarLayout = QVBoxLayout()
        self.calendarTopRow = QHBoxLayout()
        self.calendarCenter = QHBoxLayout()
        self.calendarCenterLeft = QVBoxLayout()
        self.calendarCenterRight = QVBoxLayout()
        self.calendarBottomRow = QHBoxLayout()
        self.calendar = CalendarWidget()
        self.calendar.selectionChanged.connect(self.calendarSelection)
        self.calendarLine1 = QFrame()
        self.calendarLine1.setFrameShape(QFrame.HLine)
        self.calendarWeekLabel = QLabel('To-do during week of ...:')
        self.calendarWeekDetails = QTableWidget()
        self.calendarWeekDetails.setColumnCount(6)
        self.calendarWeekDetails.setHorizontalHeaderLabels(['SN','Model','Description','Location','Cal Vendor','Cal due by'])
        self.calendarWeekDetails.setEditTriggers(QAbstractItemView.NoEditTriggers)
        self.calendarWeekDetails.setSelectionBehavior(QAbstractItemView.SelectRows)
        self.calendarWeekDetails.itemSelectionChanged.connect(self.updateCalendarAction)
        self.calendarWeekDetails.mousePressEvent = self.weekDetailsPressed
        self.calendarActionText = QLineEdit()
        self.calendarActionText.setReadOnly(True)
        self.calendarLine2 = QFrame()
        self.calendarLine2.setFrameShape(QFrame.HLine)
        self.calendarDayLabel = QLabel('Due on ...')
        self.calendarDayDetails = QTableWidget()
        self.calendarDayDetails.setColumnCount(5)
        self.calendarDayDetails.setHorizontalHeaderLabels(['SN','Model','Description','Location','Cal Vendor'])
        self.calendarDayDetails.setEditTriggers(QAbstractItemView.NoEditTriggers)
        self.calendarDayDetails.setSelectionBehavior(QAbstractItemView.SelectRows)
        self.calendarDayDetails.mousePressEvent = self.dayDetailsPressed
        self.calendarWeekMenu = QMenu(self.calendarWeekDetails)
        self.calendarDayMenu = QMenu(self.calendarDayDetails)
        self.dayGoToItem = QAction("Go to item")
        self.dayGoToItem.triggered.connect(self.dayGoTo)
        self.weekGoToItem = QAction("Go to item")
        self.weekGoToItem.triggered.connect(self.weekGoTo)
        self.calendarWeekMenu.addAction(self.weekGoToItem)
        self.calendarDayMenu.addAction(self.dayGoToItem)
        

        #Create the layouts
        self.layout = QVBoxLayout()
        self.toprow = QHBoxLayout()
        self.focus_layout = QGridLayout()
        #self.form_layout = QFormLayout()
        self.bottomrow = QGridLayout()
        #Menu Bar
        self.menubar = self.menuBar()

        exitaction = QAction("Exit",self)
        exitaction.triggered.connect(sys.exit)
        migration = QAction("Migrate Data", self)
        migration.triggered.connect(migrate)
        groups = QAction("Edit Item Groups", self)
        groups.triggered.connect(self.groupBoxShow)
        filemenu = self.menubar.addMenu("&File")
        filemenu.addAction(exitaction)
        toolsmenu = self.menubar.addMenu("&Tools")
        toolsmenu.addAction(migration)
        toolsmenu.addAction(groups)
        #Create the top row buttons to go into the sublayout (which is then in the main layout)
        self.calendarButton = QPushButton('')
        self.calendarButton.setFixedSize(25,25)
        self.calendarButton.setIcon(self.calendarIcon)
        self.calendarButton.setIconSize(QSize(21,21))
        self.calendarButton.clicked.connect(self.switchView)
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
        #Calendar top row buttons
        self.itemsButton = QPushButton('')
        self.itemsButton.setFixedSize(25,25)
        self.itemsButton.setIcon(self.toolsIcon)
        self.itemsButton.setIconSize(QSize(20,20))
        self.itemsButton.clicked.connect(self.switchView)
        self.calendarBtn0 = QPushButton('Open Cal List')
        self.calendarBtn0.clicked.connect(self.calListClick)
        self.calendarBtn2 = QPushButton('Out of Cal Report')
        self.calendarBtn2.clicked.connect(self.calReportClick)
        self.calendarBtn3 = QPushButton('Update Items')
        self.calendarBtn3.clicked.connect(self.updateClick)
        self.calendarBtn4 = QPushButton('Calibrations Folder')
        self.calendarBtn4.clicked.connect(self.calFolderClick)
        self.calendarBtn5 = QPushButton('Settings')
        self.calendarBtn5.clicked.connect(self.settingsClick)

        #Add the buttons to the sublayout
        self.toprow.addWidget(self.calendarButton)
        self.toprow.addWidget(self.btn0)
        self.toprow.addWidget(self.btn2)
        self.toprow.addWidget(self.btn3)
        self.toprow.addWidget(self.btn4)
        self.toprow.addWidget(self.btn5)

        #Calendar top row buttons
        self.calendarTopRow.addWidget(self.itemsButton)
        self.calendarTopRow.addWidget(self.calendarBtn0)
        self.calendarTopRow.addWidget(self.calendarBtn2)
        self.calendarTopRow.addWidget(self.calendarBtn3)
        self.calendarTopRow.addWidget(self.calendarBtn4)
        self.calendarTopRow.addWidget(self.calendarBtn5)

        #Calendar bottom row buttons
        self.calendarInServiceCheck = QCheckBox('Show only "In Service" items')
        self.calendarInServiceCheck.stateChanged.connect(self.calendarShowItems)
        self.calendarShowOnly = QComboBox(self)
        self.calendarOptions = {'Show All':0,'Production Equipment':1,'Quality Equipment':2,'Engineering Equipment':3}
        self.calendarShowOnly.addItems(sorted(list(self.calendarOptions.keys())))
        self.calendarShowOnly.setCurrentIndex(3)
        self.calendarShowOnly.currentIndexChanged.connect(self.calendarShowItems)

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
        self.topitems[2].setText(0,'Quality Equipment')
        self.topitems.append(QTreeWidgetItem(self.itemslist))
        self.topitems[3].setText(0,'Reference Equipment')
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
        self.groupEdit = QComboBox(self)
        self.groupEdit.setEnabled(False)
        self.groupEdit.setEditable(True)
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
        self.details.addRow('Item Group: ', self.groupEdit)
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
        self.movebtn = QPushButton('Move Item')
        self.findentry = QLineEdit(self)

        self.editItemBtn.clicked.connect(self.editClick)
        self.openfolderbtn.clicked.connect(self.openFolderClick)
        self.newreportbtn.clicked.connect(self.newReportClick)
        self.removebtn.clicked.connect(self.removeItemClick)
        self.movebtn.clicked.connect(self.moveItemClick)
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
        self.bottomrow.addWidget(self.removebtn,0,4)
        self.bottomrow.setColumnStretch(4,1)
        self.bottomrow.addWidget(self.movebtn,0,5)
        self.bottomrow.setColumnStretch(5,1)

        #Add the sublayouts to the main layout and calendar layout
        self.calendarLayout.addLayout(self.calendarTopRow)
        self.calendarCenter.addLayout(self.calendarCenterLeft)
        self.calendarCenter.addLayout(self.calendarCenterRight,2)
        self.calendarLayout.addLayout(self.calendarCenter)
        self.calendarLayout.addLayout(self.calendarBottomRow)

        self.calendarCenterLeft.addWidget(self.calendar)
        self.calendarCenterLeft.addWidget(self.calendarInServiceCheck)
        self.calendarCenterLeft.addWidget(self.calendarShowOnly)
        self.calendarCenterRight.addWidget(self.calendarWeekLabel)
        self.calendarCenterRight.addWidget(self.calendarWeekDetails)
        self.calendarCenterRight.addWidget(self.calendarActionText)
        self.calendarCenterRight.addWidget(self.calendarLine1)
        self.calendarCenterRight.addWidget(self.calendarDayLabel)
        self.calendarCenterRight.addWidget(self.calendarDayDetails)
        self.calendarCenterRight.addWidget(self.calendarLine2)
        calendarWidget.setLayout(self.calendarLayout)
        calendarWidget.setHidden(True)

        self.layout.addLayout(self.toprow)
        self.layout.addLayout(self.focus_layout)
        self.layout.addLayout(self.bottomrow)

        centralwidget.setLayout(self.layout)
        self.editable = False
    def settingsWindow(self):
        self.settings = QWidget()
        self.settings.setMinimumSize(600,300)
        self.settings.setWindowTitle('CalTools Settings')
        self.settings.setWindowIcon(self.icon)
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
        self.remove.setWindowIcon(self.icon)
        self.remove.mainText = QLabel('')

        self.remove.removeBtn = QPushButton('Remove')
        self.remove.removeBtn.clicked.connect(self.removeClick)
        self.remove.referenceBtn = QPushButton('Reference')
        self.remove.referenceBtn.clicked.connect(self.referenceClick)
        self.remove.deleteBtn = QPushButton('DELETE')
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
    def moveBox(self):
        self.moveWidget = QWidget()
        self.moveWidget.setFixedSize(350,100)
        self.moveWidget.setWindowTitle('Move Item')
        self.moveWidget.layout = QVBoxLayout()
        self.moveWidget.bottomButtons = QHBoxLayout()
        self.moveWidget.setWindowIcon(self.icon)

        self.moveWidget.mainText = QLabel('Move item to Production, Engineering, or Quality?')

        self.moveWidget.productionButton = QPushButton("Production")
        self.moveWidget.productionButton.clicked.connect(self.moveProd)
        self.moveWidget.engButton = QPushButton("Engineering")
        self.moveWidget.engButton.clicked.connect(self.moveEng)
        self.moveWidget.qualityButton = QPushButton("Quality")
        self.moveWidget.qualityButton.clicked.connect(self.moveQuality)
        self.moveWidget.cancelBtn = QPushButton('Cancel')
        self.moveWidget.cancelBtn.clicked.connect(self.moveCancel)

        self.moveWidget.bottomButtons.addWidget(self.moveWidget.cancelBtn)
        self.moveWidget.bottomButtons.addWidget(self.moveWidget.productionButton)
        self.moveWidget.bottomButtons.addWidget(self.moveWidget.engButton)
        self.moveWidget.bottomButtons.addWidget(self.moveWidget.qualityButton)
        self.moveWidget.layout.addWidget(self.moveWidget.mainText)
        self.moveWidget.layout.addLayout(self.moveWidget.bottomButtons)
        self.moveWidget.setLayout(self.moveWidget.layout)
    def groupBox(self):
        self.groupWidget = QWidget()
        self.groupWidget.setFixedSize(700,400)
        self.groupWidget.setWindowTitle('Item Groups')
        self.groupWidget.setWindowIcon(self.icon)
        self.groupWidget.layout = QGridLayout()
        self.groupWidget.currentGroupItems = []

        self.groupWidget.groupList = QListWidget()
        #self.groupWidget.groupList.itemClicked.connect(self.updateGroupDetails)
        self.groupWidget.groupList.currentItemChanged.connect(self.updateGroupDetails)

        self.groupWidget.groupDetails = QFormLayout()
        self.groupWidget.groupDetails.groupName = QLineEdit()
        self.groupWidget.groupDetails.groupName.setReadOnly(True)
        self.groupWidget.groupDetails.includeSameModel = QCheckBox()
        self.groupWidget.groupDetails.includeSameModel.setChecked(False)
        self.groupWidget.groupDetails.includeSameModel.setEnabled(False)

        self.groupWidget.groupDetails.itemList = QListWidget()
        self.groupWidget.groupDetails.itemList.setDisabled(True)
        self.contextMenu = QMenu(self.groupWidget.groupDetails.itemList)
        removeGroupListItem = QAction("Remove from group",self)
        removeGroupListItem.triggered.connect(self.removeFromList)
        self.contextMenu.addAction(removeGroupListItem)
        self.groupWidget.groupDetails.itemList.mousePressEvent = self.listMousePressed

        self.groupWidget.groupDetails.addRow('Group Name: ', self.groupWidget.groupDetails.groupName)
        self.groupWidget.groupDetails.addRow('Add items with same model: ', self.groupWidget.groupDetails.includeSameModel)
        self.groupWidget.groupDetails.addRow('Items in group: ', self.groupWidget.groupDetails.itemList)

        self.groupWidget.bottomButtons = QHBoxLayout()
        self.groupWidget.editButton = QPushButton('Edit Group')
        self.groupWidget.editButton.clicked.connect(self.editGroupClick)
        self.groupWidget.removeButton = QPushButton('Remove Group')
        self.groupWidget.removeButton.clicked.connect(self.removeGroup)

        self.groupWidget.bottomButtons.addWidget(self.groupWidget.editButton)
        self.groupWidget.bottomButtons.addWidget(self.groupWidget.removeButton)

        self.groupWidget.layout.addWidget(self.groupWidget.groupList,0,0)
        self.groupWidget.layout.addLayout(self.groupWidget.groupDetails,0,1)
        self.groupWidget.layout.addLayout(self.groupWidget.bottomButtons,1,1)
        self.groupWidget.layout.setColumnStretch(1,1)
        self.groupWidget.setLayout(self.groupWidget.layout)

    #Mouse button clicks and context menu events for the group's items list
    def listMousePressed(self,event):
        if event.button() == Qt.MouseButton.RightButton:
            self.groupWidget.groupDetails.itemList.setCurrentItem(self.groupWidget.groupDetails.itemList.itemAt(event.pos()))
            self.contextMenu.popup(event.globalPos())
        elif event.button() == Qt.MouseButton.LeftButton:
            self.groupWidget.groupDetails.itemList.setCurrentItem(self.groupWidget.groupDetails.itemList.itemAt(event.pos()))

    @Slot()
    def removeFromList(self):
        item = self.groupWidget.groupDetails.itemList.currentItem().text()
        row = self.groupWidget.groupDetails.itemList.currentRow()
        self.groupWidget.currentGroupItems.remove(item)
        self.groupWidget.groupDetails.itemList.takeItem(row)
    #Slots for buttons-------------------------------------------------------------------------------------------------------------
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
        currentItem = self.itemslist.currentItem().text(0)
        searchmode = self.searchoptionsdict[self.searchoptions.currentText()]
        text = self.findentry.text()
        self.updateTree(search = True, text = text, mode = searchmode)
        self.goToListItem(currentItem)
    @Slot()
    def calFolderClick(self):
        startfile(calScansDir)
    @Slot()
    def settingsClick(self):        
        #Display current set directories
        self.settings.calDirecEdit.setText(calListDir)
        self.settings.tempDirecEdit.setText(tempFilesDir)
        self.settings.scansDirecEdit.setText(calScansDir)
        self.settings.setWindowModality(Qt.ApplicationModal)
        self.settings.show()
    @Slot()
    def removeItemClick(self):
        if self.itemslist.currentItem():
            if self.itemslist.currentItem().text(0) != None and self.itemslist.currentItem().text(0) != '':
                self.remove.mainText.setText('Item {}: \nRemove from service, reference only, or delete from DB?'.format(self.itemslist.currentItem().text(0)))
                self.remove.mainText.setAlignment(Qt.AlignHCenter)
                self.remove.setWindowModality(Qt.ApplicationModal)
                self.remove.show()
                self.remove.raise_()
    @Slot()
    def moveItemClick(self):
        connect()
        try:
            for item in allItems():
                if item[0] == self.itemslist.currentItem().text(0):
                    self.moveWidget.mainText.setText('Move {} to Production, Engineering, or Quality?'.format(item[0]))
                    self.moveWidget.mainText.setAlignment(Qt.AlignHCenter)
                    self.moveWidget.setWindowModality(Qt.ApplicationModal)
                    disconnect()
                    self.moveWidget.show()
                    self.moveWidget.raise_()
                    break
        except AttributeError:
            disconnect(save = False)
            return
    #Calendar slots------------------------------------------------------------------------------------------------------------------------
    @Slot()
    def switchView(self):
        global centralwidget,calendarWidget
        if calendarWidget.isHidden():
            self.calendar.dates = self.calendar.updateCalendar()
            centralwidget = self.takeCentralWidget()
            self.setCentralWidget(calendarWidget)
            calendarWidget.setHidden(False)
        else:
            calendarWidget = self.takeCentralWidget()
            self.setCentralWidget(centralwidget)
            calendarWidget.setHidden(True)
    @Slot()
    def calendarShowItems(self):
        self.calendar.dates = self.calendar.updateCalendar(self.calendarShowOnly.currentIndex(),self.calendarInServiceCheck.isChecked())
        self.calendar.updateCells()
        self.calendarSelection()
    @Slot()
    def calendarSelection(self):
        index = self.calendarShowOnly.currentIndex()
        selectedDate = self.calendar.selectedDate()
        date = '{}-{}-{}'.format(selectedDate.year(),selectedDate.month(),selectedDate.day())
        weekOf = ''
        weekStart = QDate(selectedDate.year(),selectedDate.month(),selectedDate.day())
        while weekStart.dayOfWeek() != 7:
            weekStart = weekStart.addDays(-1)
        self.calendarWeekLabel.setText('To-do during week of {}:'.format(weekStart.toString(Qt.ISODate)))
        self.calendarDayLabel.setText('Due on {}: '.format(date))
        self.calendarWeekDetails.clear()
        self.calendarWeekDetails.setHorizontalHeaderLabels(['SN','Model','Description','Location','Cal Vendor','Cal due by'])
        self.calendarDayDetails.clear()
        self.calendarDayDetails.setHorizontalHeaderLabels(['SN','Model','Description','Location','Cal Vendor'])

        #Update the day table
        dayRow = 0
        weekRow = 0
        self.calendarDayDetails.setRowCount(0)
        self.calendarWeekDetails.setRowCount(0)
        connect()
        for item in allItems():
            if index == 0:
                if 'ENGINEERING EQUIPMENT' not in item[8]:
                    continue
            elif index == 1:
                if 'PRODUCTION EQUIPMENT' not in item[8]:
                    continue
            elif index == 2:
                if 'QUALITY EQUIPMENT' not in item[8]:
                    continue
            if self.calendarInServiceCheck.isChecked():
                if item[10] != 1:
                    continue
            #Day items
            if item[6] == selectedDate.toString(Qt.ISODate):
                self.calendarDayDetails.insertRow(dayRow)
                self.calendarDayDetails.setItem(dayRow,0,QTableWidgetItem(item[0]))
                self.calendarDayDetails.setItem(dayRow,1,QTableWidgetItem(item[14]))
                self.calendarDayDetails.setItem(dayRow,2,QTableWidgetItem(item[9]))
                self.calendarDayDetails.setItem(dayRow,3,QTableWidgetItem(item[1]))
                self.calendarDayDetails.setItem(dayRow,4,QTableWidgetItem(item[3]))
                dayRow += 1
            
            #Week items
            if item[6] == '' or item[3] == 'Perceptron':
                continue
            if (datetime.strptime(item[6],'%Y-%m-%d').date() - datetime.strptime(weekStart.toString(Qt.ISODate),'%Y-%m-%d').date()).days <= 60:
                if item[7] == 0:
                    self.calendarWeekDetails.insertRow(weekRow)
                    self.calendarWeekDetails.setItem(weekRow,0,QTableWidgetItem(item[0]))
                    self.calendarWeekDetails.setItem(weekRow,1,QTableWidgetItem(item[14]))
                    self.calendarWeekDetails.setItem(weekRow,2,QTableWidgetItem(item[9]))
                    self.calendarWeekDetails.setItem(weekRow,3,QTableWidgetItem(item[1]))
                    self.calendarWeekDetails.setItem(weekRow,4,QTableWidgetItem(item[3]))
                    self.calendarWeekDetails.setItem(weekRow,5,QTableWidgetItem(item[6]))
                    weekRow += 1
        disconnect()
    @Slot()
    def updateCalendarAction(self):
        self.calendarActionText.clear()
        connect()
        for item in allItems():
            try:
                if item[0] == self.calendarWeekDetails.selectedItems()[0].text():
                    self.calendarActionText.setText(checkReplacements(item))
                    break
            except IndexError:
                pass
        disconnect()
    @Slot()
    def weekDetailsPressed(self,event):
        if event.button() == Qt.MouseButton.RightButton:
            self.calendarWeekDetails.setCurrentItem(self.calendarWeekDetails.itemAt(event.pos()))
            self.calendarWeekMenu.popup(event.globalPos())
        elif event.button() == Qt.MouseButton.LeftButton:
            self.calendarWeekDetails.setCurrentItem(self.calendarWeekDetails.itemAt(event.pos()))
    @Slot()
    def dayDetailsPressed(self,event):
        if event.button() == Qt.MouseButton.RightButton:
            self.calendarDayDetails.setCurrentItem(self.calendarDayDetails.itemAt(event.pos()))
            self.calendarDayMenu.popup(event.globalPos())
        elif event.button() == Qt.MouseButton.LeftButton:
            self.calendarDayDetails.setCurrentItem(self.calendarDayDetails.itemAt(event.pos()))
    @Slot()
    def dayGoTo(self):
        sn = self.calendarDayDetails.selectedItems()[0].text()
        self.switchView()
        self.goToListItem(sn)
    @Slot()
    def weekGoTo(self):
        sn = self.calendarWeekDetails.selectedItems()[0].text()
        self.switchView()
        self.goToListItem(sn)
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
    def settingsCancel(self):
        global newdbDir
        load()
        self.settings.hide()

    @Slot()
    def settingsOk(self):
        global dbDir, config
        if calListDir not in dbDir:
            try:
                copyfile(dbDir,calListDir)
            except Exception as e:
                errorMessage('Error!', str(e))

        dbDir = calListDir + '\\calibrations.db'
        lines = []
        with open('config.py','rt') as cfg:
            for line in cfg:
                lines.append(line)
        with open('config.py','wt',encoding = "UTF-8") as cfg:
            for line in lines:
                if "calListDir" in line:
                    line = 'calListDir = r"{}"\n'.format(calListDir)
                elif "tempFilesDir" in line:
                    line = 'tempFilesDir = r"{}"\n'.format(tempFilesDir)
                elif "calScansDir" in line:
                    line = 'calScansDir = r"{}"\n'.format(calScansDir)
                cfg.write(line)
        #reload(config)
        config = SourceFileLoader('config','config.py').load_module()
        load()
        self.settings.hide()


    #Item Removal Button Slots-------------------------------------------------------------------------------------------------------------
    @Slot()
    def removeClick(self): #Remove an item from service, moving its documents to the "Removed from Service" folder.
        connect()
        for item in allItems():
            if self.itemslist.currentItem().text(0) == item[0]:
                try:
                    newDirec = '{}\\Calibration Items\\Removed from Service'.format(calScansDir)
                    if not path.isdir(item[8]):
                        errorMessage('Error moving item: {}'.format(item[0]),'The directory on record for this item does not exist.')
                        disconnect(save = False)
                        return
                    else:
                        move(item[8],newDirec)
                        c.execute("UPDATE calibration_items SET directory='{}' WHERE serial_number=?".format(newDirec),(item[0],))
                except FileNotFoundError:
                    pass
                removeFromGroups(item[0])
                self.remove.hide()
        disconnect()
        self.updateTree()
    @Slot()
    def referenceClick(self): #Put an item in the "Reference only" folder, update the directory in the DB
        connect()
        for item in allItems():
            if self.itemslist.currentItem().text(0) == item[0]:
                try:
                    newDirec = '{}\\Calibration Items\\Ref Only'.format(calScansDir)
                    if not path.isdir(item[8]):
                        errorMessage('Error moving item: {}'.format(item[0]),'The directory on record for this item does not exist.')
                        disconnect(save = False)
                        return
                    else:
                        move(item[8],newDirec)
                        c.execute("UPDATE calibration_items SET directory='{}' WHERE serial_number=?".format(newDirec),(item[0],))
                except FileNotFoundError:
                    pass
                removeFromGroups(item[0])
                self.remove.hide()   
        disconnect()
        self.updateTree()
    @Slot()
    def deleteClick(self):
        connect()
        for item in allItems():
            if self.itemslist.currentItem().text(0) == item[0]:
                warningMessage = QMessageBox()
                warningMessage.setWindowTitle('Delete "{}"?'.format(item[0]))
                warningMessage.setText('This function will DELETE "{}" from the database. Continue?'.format(item[0]))
                okButton = warningMessage.addButton(QMessageBox.Ok)
                cancelButton = warningMessage.addButton(QMessageBox.Cancel)
                warningMessage.exec_()
                if warningMessage.clickedButton() == cancelButton:
                    self.remove.hide()
                    disconnect()
                    return
                elif warningMessage.clickedButton() == okButton:
                    try:
                        #rmtree(item[8])
                        removeFromDB(item[0])
                    except Exception as e:
                        errorMessage('Error!', str(e))
                        pass
                    try:
                        removeFromDB(item[0])
                    except Exception as e:
                        errorMessage('Error!',str(e))
                break
        disconnect()
        self.updateGroupList()
        self.updateTree()
        self.remove.hide()
    @Slot()
    def removeCancel(self):
        self.remove.hide()

    #Move item slots-----------------------------------------------------------------------------------------------------------------------
    @Slot()
    def moveItem(self,selection):
        if selection == 0:
            folder = "PRODUCTION EQUIPMENT"
        elif selection == 1:
            folder = "ENGINEERING EQUIPMENT"
        elif selection == 2:
            folder = "QUALITY EQUIPMENT"
        newDirec = '{}\\Calibration Items\\{}'.format(calScansDir,folder)

        connect()
        for item in allItems():
            if self.itemslist.currentItem().text(0) == item[0]:
                try:
                    if not path.isdir(item[8]):
                        errorMessage('Error moving item: {}'.format(item[0]),'The directory on record for this item does not exist.')
                        disconnect(save = False)
                        return
                    else:
                        move(item[8],newDirec)
                        c.execute("UPDATE calibration_items SET directory='{}' WHERE serial_number=?".format(newDirec),(item[0],))
                except Exception as e:
                    errorMessage('Error!',str(e))
                    disconnect()
                    return
        disconnect()
        self.updateTree()
        self.moveWidget.hide()

    @Slot()
    def moveProd(self):
        self.moveItem(0)
    @Slot()
    def moveEng(self):
        self.moveItem(1)
    @Slot()
    def moveQuality(self):
        self.moveItem(2)
    @Slot()
    def moveCancel(self):
        self.moveWidget.hide()

    #Group box slots/methods-----------------------------------------------------------------------------------------------------------------------
    @Slot()
    def groupBoxShow(self):
        self.updateGroupList()
        self.groupWidget.show()

    @Slot()
    def removeGroup(self):
        connect()
        selectedGroup = None
        try:
            selectedGroup = self.groupWidget.groupList.currentItem().text()
            for g in allGroups():
                if g[0] == selectedGroup:
                    selectedGroup = g
                    break
            if selectedGroup == None:
                return
            groupItems = json.loads(selectedGroup[1])
            all_items = allItems()
            #Create 2nd cursor so that the loop doesn't break
            removeGroupC = conn.cursor()
            for item in all_items:
                if item[0] in groupItems or item[17] == selectedGroup[0]:
                    removeGroupC.execute("UPDATE calibration_items SET item_group='' WHERE serial_number=?",(item[0],))
            c.execute("DELETE FROM item_groups WHERE name=?",(selectedGroup[0],))
            disconnect()
        except Exception as e:
            errorMessage('Error removing group',str(e))
            disconnect(save = False)
        self.updateGroupList()
    @Slot()
    def addAllModel(self,group):
        pass
    def updateGroupList(self):
        self.groupWidget.groupList.clear()
        connect()
        for group in allGroups():
            self.groupWidget.groupList.addItem(group[0])
        for item in allItems():
            if item[17] != '':
                for group in allGroups():
                    if item[17] == group[0]:
                        if item[0] not in json.loads(group[1]) and 'Ref Only' not in item[8]:
                            addToGroup(item[0],group[0])
        disconnect()
    @Slot()
    def updateGroupDetails(self):
        if self.groupWidget.editButton.text() == 'Save':
            self.groupWidget.editButton.setText('Edit Group')
            self.groupWidget.groupDetails.groupName.setReadOnly(True)
            self.groupWidget.groupDetails.includeSameModel.setEnabled(False)
            self.groupWidget.groupDetails.itemList.setDisabled(True)
        self.groupWidget.groupDetails.groupName.clear()
        self.groupWidget.groupDetails.includeSameModel.setChecked(False)
        self.groupWidget.groupDetails.itemList.clear()
        self.groupWidget.currentGroupItems = []
        try:
            selectedItem = self.groupWidget.groupList.currentItem().text()
        except AttributeError:
            return
        connect()
        group = None
        for g in allGroups():
            if selectedItem == g[0]:
                group = g
                break
        disconnect()
        self.groupWidget.groupDetails.groupName.setText(g[0])
        if g[2] == 1:
            self.groupWidget.groupDetails.includeSameModel.setChecked(True)
        self.groupWidget.groupDetails.itemList.addItems(sorted(json.loads(g[1])))
        self.groupWidget.currentGroupItems = json.loads(g[1])
    #--------------------------------------------------------------------------------------------------------------------------------------
    def goToListItem(self,sn):
        for item in self.productionItems:
            if item.text(0) == sn:
                self.itemslist.setCurrentItem(item)
                return
        for item in self.engineeringItems:
            if item.text(0) == sn:
                self.itemslist.setCurrentItem(item)
                return
        for item in self.qualityItems:
            if item.text(0) == sn:
                self.itemslist.setCurrentItem(item)
                return
        for item in self.refItems:
            if item.text(0) == sn:
                self.itemslist.setCurrentItem(item)
                return
    @Slot()
    def editGroupClick(self):
        try:
            currentGroup = self.groupWidget.groupList.currentItem().text()
        except AttributeError:
            return
        #If not in edit mode
        if self.groupWidget.editButton.text() == 'Edit Group':
            self.groupWidget.groupDetails.groupName.setReadOnly(False)
            self.groupWidget.groupDetails.includeSameModel.setEnabled(True)
            self.groupWidget.groupDetails.itemList.setDisabled(False)
            self.groupWidget.editButton.setText('Save')
        #If in edit mode
        else:
            #Update the DB
            connect()
            editGroupClickC = conn.cursor()
            for g in allGroups():
                if g[0] == currentGroup:
                    name = self.groupWidget.groupDetails.groupName.text()
                    groupItems = json.loads(g[1])
                    if name != g[0]:
                        for item in groupItems:
                            editGroupClickC.execute("UPDATE calibration_items SET item_group='{}' WHERE serial_number=?".format(name),(item,))
                    if self.groupWidget.groupDetails.includeSameModel.isChecked():
                        allModel = 1
                    else:
                        allModel = 0
                    for itemSN in groupItems:
                        if itemSN not in self.groupWidget.currentGroupItems:
                            editGroupClickC.execute("UPDATE calibration_items SET item_group='' WHERE serial_number=?",(itemSN,))
                    c.execute("UPDATE item_groups SET name='{}',items='{}',include_all_model={} WHERE name=?".format(name,json.dumps(self.groupWidget.currentGroupItems),allModel),(currentGroup,))
                    if allModel == 1:
                        for groupItem in self.groupWidget.currentGroupItems:
                            for item in allItems():
                                if item[0] == groupItem:
                                    modelItem = item
                                    break
                            for item2 in allItems():
                                if item2[14] == modelItem[14]:
                                    addToGroup(item2[0],name)            
            self.groupWidget.groupDetails.groupName.setReadOnly(True)
            self.groupWidget.groupDetails.includeSameModel.setEnabled(False)
            self.groupWidget.groupDetails.itemList.setDisabled(True)
            self.groupWidget.editButton.setText('Edit Group')
            disconnect()
            self.updateGroupList()
            self.groupWidget.groupList.setCurrentItem(self.groupWidget.groupList.findItems(name,Qt.MatchExactly)[0])
    #Slots for item details----------------------------------------------------------------------------------------------------------------
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
                    self.groupEdit.clear()

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
                    #Handles showing groups------------------------------------------------------------
                    groups = []
                    for g in allGroups():
                        groups.append(g[0])
                    self.groupEdit.addItems(sorted(groups))
                    self.groupEdit.setCurrentText(item[17])
                    #----------------------------------------------------------------------------------
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
            errorMessage('Error showing item details',str(e))
           
    #Bottom row button slots---------------------------------------------------------------------------------------------------------------
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
        self.topitems[2].setExpanded(True)

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
            errorMessage('Error opening folder', str(e))
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
            self.groupEdit.setEnabled(True)
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
                        group = self.groupEdit.currentText()
                        groupExists = False
                        #Creates the above group if it does not exist
                        for g in allGroups():
                            if group == g:
                                groupExists = True
                                break
                        if not groupExists and group != '':
                            c.execute("""INSERT OR IGNORE INTO item_groups (name)
                                        VALUES (?)""", (group,))
                        if group == '' and item[17] != '':
                            removeFromGroups(item[0])
                        #Add this item to the group's item list, check for error
                        if group != '':
                            groupSuccess = addToGroup(sn,group)
                            if groupSuccess == 0 or groupSuccess == 1:
                                disconnect()
                                self.updateGroupList()
                                connect()
                                pass
                            elif groupSuccess == 2 or groupSuccess == 3:
                                group = ''

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
                            if inservicedate != '' and item[5] != '':
                                nextcal = str(datetime.strptime(item[5],'%Y-%m-%d').date() + relativedelta(months = interval))
                                #Removing the use of "in service" dates, in favor of better scheduling of calibration
                                #nextcal = str(datetime.strptime(inservicedate,'%Y-%m-%d').date() + relativedelta(months =+ item[2]))
                            else:
                                nextcal = ''
                                #nextcal = str(datetime.strptime(item[5],'%Y-%m-%d').date() + relativedelta(months = interval))
                            outofservicedate = ''
                        elif item[10] == 1 and self.inServiceEdit.isChecked() == False:
                            inservice = 0
                            outofservicedate = datetime.strftime(datetime.today().date(),'%Y-%m-%d')
                            inservicedate = ''
                            nextcal = str(datetime.strptime(item[5],'%Y-%m-%d').date() + relativedelta(months = interval))
                        else:
                            inservice = 0
                            nextcal = str(datetime.strptime(item[5],'%Y-%m-%d').date() + relativedelta(months = interval))
                            inservicedate = ''
                            outofservicedate = item[12]
                        selecteditem = self.itemslist.currentItem().text(0)
                        timestamp = datetime.utcnow().strftime('%Y-%m-%d-%H-%M-%S-%f')
                        sql = """
                                UPDATE calibration_items SET serial_number='{}',model='{}',description='{}',location='{}',manufacturer='{}',cal_vendor='{}',interval={},calasneeded={},inservice={},inservicedate='{}',
                                nextcal='{}',outofservicedate='{}',comment='{}',timestamp='{}',item_group='{}' WHERE serial_number=?
                                """.format(sn,model,description,location,manufacturer,cal_vendor,interval,calasneeded,inservice,inservicedate,nextcal,outofservicedate,comment,timestamp,group)
                        c.execute(sql,(sn,))
                        disconnect()
                        if self.findentry.text() != '' or self.searchoptions.currentIndex() == 6 or self.searchoptions.currentIndex() == 2:
                            self.find()
                        else:
                            self.updateTree(selecteditem)
                        #Re-select item
                        self.goToListItem(selecteditem)
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
            self.groupEdit.setEnabled(False)
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

    #Updates the list of items in the tree----------------------------------------------------------------------------------------------------
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
        self.topitems[3].takeChildren()
        self.productionItems = []
        self.engineeringItems = []
        self.qualityItems = []
        self.refItems = []
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
                elif 'QUALITY EQUIPMENT' in item[8]:
                    if search == False:
                        self.qualityItems.append(QTreeWidgetItem(self.topitems[2]))
                        self.qualityItems[-1].setText(0, item[0])
                    else:
                        if item in searchItems and mode != 13 and mode != 15:
                            self.qualityItems.append(QTreeWidgetItem(self.topitems[2]))
                            self.qualityItems[-1].setText(0, item[0])
                        elif mode == 13:
                            if item[13] != 0:
                                self.qualityItems.append(QTreeWidgetItem(self.topitems[2]))
                                self.qualityItems[-1].setText(0, item[0])
                        elif mode == 15:
                            if item[15] != '':
                                self.qualityItems.append(QTreeWidgetItem(self.topitems[2]))
                                self.qualityItems[-1].setText(0, item[0])

                elif 'Ref Only' in item[8]:
                    if search == False:
                        self.refItems.append(QTreeWidgetItem(self.topitems[3]))
                        self.refItems[-1].setText(0, item[0])
                    else:
                        if item in searchItems and mode != 13 and mode != 15:
                            self.refItems.append(QTreeWidgetItem(self.topitems[3]))
                            self.refItems[-1].setText(0, item[0])
                        elif mode == 13:
                            if item[13] != 0:
                                self.refItems.append(QTreeWidgetItem(self.topitems[3]))
                                self.refItems[-1].setText(0, item[0])
                        elif mode == 15:
                            if item[15] != '':
                                    self.refItems.append(QTreeWidgetItem(self.topitems[3]))
                                    self.refItems[-1].setText(0, item[0])
        disconnect(save = False)

        self.topitems[0].addChildren(self.productionItems)
        self.topitems[0].sortChildren(0,Qt.AscendingOrder)
        self.topitems[1].addChildren(self.engineeringItems)
        self.topitems[1].sortChildren(0,Qt.AscendingOrder)
        self.topitems[2].addChildren(self.qualityItems)
        self.topitems[2].sortChildren(0,Qt.AscendingOrder)
        self.topitems[3].addChildren(self.refItems)
        self.topitems[3].sortChildren(0,Qt.AscendingOrder)
        return

    def checkFirstRun(self):
        global config
        if config.firstRun == True:
            successMessage('First start up', """
It appears that this is the first time CalTools has been run here. 
Please check the directories in settings to make sure they're correct.
Alternatively, the config.py text file bundled with this executable can be edited directly.
            """)
        lines = []
        if path.isfile('config.py'):
            with open('config.py','rt') as cfg:
                for line in cfg:
                    lines.append(line)
            with open('config.py','wt',encoding = "UTF-8") as cfg:
                for line in lines:
                    if "firstRun" in line:
                        line = 'firstRun = False'
                    cfg.write(line)
        #reload(config)
        config = SourceFileLoader('config','config.py').load_module()

    def run(self):
        
        self.checkFirstRun()
        try:
            connect()
        except Exception as e:
            errorMessage('Error!', str(e))
        disconnect()
        create_tables()
        load()
        updateitems()
        self.settingsWindow()
        self.removeBox()
        self.moveBox()
        self.groupBox()
        self.updateTree()
        self.show()
        qt_app.exec_()

#Generic message boxes-----------------------------------------------------------------------------------------------------
def errorMessage(title = '', text = ''):
    message = QMessageBox()
    message.setIcon(QMessageBox.Critical)
    message.setWindowTitle(title)
    message.setText(text)
    message.exec_()

def successMessage(title = '', text = ''):
    message = QMessageBox()
    message.setIcon(QMessageBox.Information)
    message.setWindowTitle(title)
    message.setText(text)
    message.exec_()

#Run-----------------------------------------------------------------------------------------------------
if __name__ == '__main__':
    app = MainWindow()
    app.run()