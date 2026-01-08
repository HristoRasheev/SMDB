# SMDB – Simple Mini Database

SMDB is a lightweight educational database system written in **C# (.NET 8)** with a **custom SQL-like parser** and a **desktop UI built with Avalonia**.  
The project is designed to demonstrate how a basic database engine works internally, without relying on existing DBMS libraries.

---

## ✨ Features

- Custom SQL-like language
- Manual query parsing (no `Split`, no regex-based SQL parsing)
- Table storage using files (`.meta` and data files)
- Supported commands:
  - `CREATE TABLE`
  - `DROP TABLE`
  - `INSERT`
  - `SELECT`
  - `DELETE`
  - `CHECK`
  - `TABLEINFO`
  - `CREATE INDEX`
  - `DROP INDEX`
- Index support on table columns
- Integrity checking
- Simple query execution engine
- Desktop UI built with **Avalonia**
- Cross-platform (Windows / macOS / Linux)

---

## 🧱 Project Structure
