
CalTools is an application to automate a part of the process of tracking and planning calibration activities. 
The goal is to allow for multiple users to interact with the data in a way that is controlled, so that required information is verified to be present, while the file system and SQLite database stored on the network are kept to a standard.

The application's main features include:
  -An XML config file that allows specifying the folders in the file system to include in the application, SQLite database name, and the time period in which items are flagged to be due for calibration
  -Search functionality, eliminating the need for a user to manually search the file structure. A user can search by a characteristic of the item(s) they are looking for, such as: serial number, description text, comments, physical location, model, manufacturer, item group, etc.
  -A calendar, which when a date is selected, triggers a corresponding table to show items that are due within X days (xml configurable) of the selected date.
  -An option to export the entirety of the database contents to a tab-separated value file.
  -Drag and drop functionality for easily adding files, such as PDF documents, to the file system, without the need to search through the folders.
  -Option to add calibration data to the database, rather than to the file system as a document.
  
  
The SQLite database is managed so that the application only maintains a connection for as long as it needs for a transaction, reducing conflicts for multiple users. Delete operations are cascading. So when an item is deleted, its associated data is also deleted. Deleting calibration data does not delete the associated item. The database contains three tables: Items, Tasks, TaskData. These tables store characteristics about the equipment (Items), tasks that require an action and/or documentation (Tasks), and the data associated with those tasks (TaskData). Serial numbers are primary keys in the Items table, Tasks and TaskData have IDs, tasks are linked to items  via foreign key, and TaskData is linked to the relevant Task ID. Data is stored as JSON objects which are serialized from the objects defined in the application.
