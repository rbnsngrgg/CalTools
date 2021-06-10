using CalTools_WPF.ObjectClasses;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;

namespace CalTools_WPF
{
    partial class CTDatabase
    {
        public enum ItemsColumns
        {
            serial_number,
            location,
            manufacturer,
            directory,
            description,
            in_service,
            model,
            item_group,
            remarks,
            is_standard_equipment,
            certificate_number,
            timestamp
        }
        public enum TasksColumns
        {
            id,
            serial_number,
            task_title,
            service_vendor,
            is_mandatory,
            interval,
            complete_date,
            due_date,
            is_due,
            action_type,
            directory,
            remarks,
            date_override
        }
        public enum StandardEquipmentColumns
        {
            id,
            serial_number,
            manufacturer,
            model,
            description,
            remarks,
            item_group,
            certificate_number,
            action_due_date,
            timestamp
        }
        public enum TaskDataColumns
        {
            id,
            task_id,
            serial_number,
            in_tolerance_before,
            operational_before,
            in_tolerance_after,
            operational_after,
            calibrated,
            verified,
            adjusted,
            repaired,
            maintenance,
            complete_date,
            procedure,
            remarks,
            technician,
            timestamp
        }
        public enum DataStandardEquipmentColumns
        {
            id,
            data_id,
            standard_equipment_id
        }
        public enum FindingsColumns
        {
            id,
            data_id,
            name,
            tolerance,
            tolerance_is_percent,
            unit_of_measure,
            measurement_before,
            measurement_after,
            setting
        }

        public enum ItemsColumnsV5
        {
            SerialNumber = 0,
            Location,
            Manufacturer,
            Directory,
            Description,
            InService,
            InServiceDate,
            Model,
            Comments,
            Timestamp,
            ItemGroup,
            StandardEquipment,
            CertificateNumber
        }
        public enum TasksColumnsV5
        {
            TaskID,
            SerialNumber,
            TaskTitle,
            ServiceVendor,
            Mandatory,
            Interval,
            CompleteDate,
            DueDate,
            Due,
            ActionType,
            Directory,
            Comments,
            ManualFlag
        }
        public enum TaskDataColumnsV5
        {
            ColDataID,
            ColTaskID,
            ColSerialNumber,
            ColStateBeforeAction,
            ColStateAfterAction,
            ColActionTaken,
            ColCompleteDate,
            ColProcedure,
            ColStandardEquipment,
            ColFindings,
            ColRemarks,
            ColTechnician,
            ColEntryTimestamp
        }
    }
}
