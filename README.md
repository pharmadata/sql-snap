# SQL Snap

SQL Snap is a tool that allows the creation of [SQL Server snapshot backups](https://technet.microsoft.com/en-us/library/ms189548(v=sql.105).aspx).

Snapshot backups allow you to snapshot databases at an infrastructure level where typical snapshot
taking and restoring is extremely quick.  This is as opposed to regular backups where the database
files are read in their entirety while writing to a backup file.

Furthermore, while other snapshotting over SQL Server databases exists, SQL Snap will keep the
backup in a "consistent" state and allow you to restore the database in recovery.  This means you
can apply transaction logs and/or tail-log backups to provide true point-in-time recovery.

Previously, it has been restricted to independent software and hardware vendors to implement
snapshot backups, which has prevented average users from leveraging the power of snapshot backups.

The tool allows you to provide your own implementation on how snapshots are taken.  We developed
this with SQL Server hosted on AWS with EBS snapshots in mind, but it'll work with any snapshot
API/tool you have.

## Usage

You can either consume the core assembly via .NET/PowerShell or use the CLI interface.

### CLI

#### Backup

usage: `sqlsnap backup [options]`

```
-i, --instanceName    Name of SQL Server instance for which to connect (optional)
-m, --metadata        Required. Path to file to store backup metadata
-d, --database        Required. Database to backup
-c, --command         Required. Command to execute that performs the snapshot
-v, --verbose         Include verbose logging information
-t, --timeout         Timeout for backup operation (in seconds, default 600)
```

#### Restore

usage `sqlsnap restore [options]`

```
-i, --instanceName    Name of SQL Server instance for which to connect (optional)
-m, --metadata        Required. Path to file to containing the backup metadata
-d, --database        Required. Database to restore
-c, --command         Command to execute that mounts the snapshot (optional -
                      not required if the database is detached and you've
                      already mounted the snapshot)                  
--noRecovery          Restore the database with the NORECOVERY option
-v, --verbose         Include verbose logging information
-t, --timeout         Timeout for backup operation (in seconds, default 600)
```

## How it works

SQL Snap takes advantage of the [Virtual Backup Device Interface (VDI)](https://www.microsoft.com/en-us/download/details.aspx?id=17282).

It uses COM Interop via .NET to create a virtual device for receiving the backup, which includes
receiving or supplying the metadata and responding to commands on freezing the database and
taking or mounting the snapshot.

The backup flow works like:

* Create the virtual device set
* Issue the `BACKUP DATABASE` command `WITH SNAPSHOT`
* Write and flush the metadata to disk
* Freeze the database to keep it in a consistent state
* Take the snapshot
* Unfreeze the database

The restore flow is similar:

* Create the virtual device set
* Issue the `RESTORE DATABASE` command `WITH SNAPSHOT`
* Read and supply the metadata from disk
* Freeze the database (if attached)
* Mount the snapshot
* Database restored

## Snapshot metadata

When you backup the snapshot, make sure the metadata associated with the snapshot is securely
stored along with the snapshot.  This is absolutely required for restoration.

## Future

This project could include some default cloud/hardware provider implementations (e.g. AWS EBS snapshots)
to allow consumers to rapidly get snapshot backups up and running.

## License

[MIT](/LICENSE.md)
