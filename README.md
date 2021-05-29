# Dapper.FSharp.Linq [![NuGet](https://img.shields.io/nuget/v/Dapper.FSharp.Linq.svg?style=flat)](https://www.nuget.org/packages/Dapper.FSharp.Linq/)

<p align="center">
<img src="https://github.com/Dzoukr/Dapper.FSharp/raw/master/logo.png" width="150px"/>
</p>

This library contains extended Linq features that _may or may not_ be pulled back into Dapper.FSharp.

These extended Linq features exist to facilitate the use of [generated records](https://github.com/JordanMarr/SqlHydra) for type safety, and were not included in Dapper.FSharp because they required changes to the core library that were too extensive, or because they did not align with the intended use case of the original library.

This readme has been modified to focus only on the Linq builders and their extended features.

## Extended Linq Features

- Ability to manually include fields via the `column` operation on the `insert` and `update` builders.
- Ability to manually exclude fields via the `exclude` operation on the `insert` and `update` builders.

## Installation
If you want to install this package manually, use usual NuGet package command 

    Install-Package Dapper.FSharp.Linq

or using [Paket](http://fsprojects.github.io/Paket/getting-started.html) 

    paket add Dapper.FSharp.Linq


## Getting started

First of all, you need to init registration of mappers for optional types to have Dapper mappings understand that `NULL` from database = `Option.None`

```f#
Dapper.FSharp.OptionTypes.register()
```

It's recommended to do it somewhere close to program entry point or in `Startup` class.

### Example database

Lets have a database table called `Persons`:

```sql
CREATE TABLE [dbo].[Persons](
    [Id] [uniqueidentifier] NOT NULL,
    [FirstName] [nvarchar](max) NOT NULL,
    [LastName] [nvarchar](max) NOT NULL,
    [Position] [int] NOT NULL,
    [DateOfBirth] [datetime] NULL)
```

As mentioned in FAQ section, you need F# record to work with such table in `Dapper.FSharp`:

```f#
type Person = {
    Id : Guid
    FirstName : string
    LastName : string
    Position : int
    DateOfBirth : DateTime option
}
```

*Hint: Check tests located under tests/Dapper.FSharp.Tests folder for more examples*

### Table Mappings
You can either specify your tables within the query, or you can specify them above your queries (which is recommended since it makes them sharable between your queries).
The following will assume that the table name exactly matches the record name, "Person":

```F#
let personTable = table<Person>
```

If your record maps to a table with a different name:

```F#
let personTable = table'<Person> "People"
```

If you want to include a schema name:

```F#
let personTable = table'<Person> "People" |> inSchema "dbo"
```

### INSERT

Inserting a single record:

```f#
open Dapper.FSharp.LinqBuilders
open Dapper.FSharp.MSSQL

let conn : IDbConnection = ... // get it somewhere

let newPerson = { Id = Guid.NewGuid(); FirstName = "Roman"; LastName = "Provaznik"; Position = 1; DateOfBirth = None }

let personTable = table<Person>

insert {
    into personTable
    value newPerson
} |> conn.InsertAsync
```

Inserting Multiple Records:

```f#
open Dapper.FSharp.LinqBuilders
open Dapper.FSharp.MSSQL

let conn : IDbConnection = ... // get it somewhere

let person1 = { Id = Guid.NewGuid(); FirstName = "Roman"; LastName = "Provaznik"; Position = 1; DateOfBirth = None }
let person2 = { Id = Guid.NewGuid(); FirstName = "Ptero"; LastName = "Dactyl"; Position = 2; DateOfBirth = None }

let personTable = table<Person>

insert {
    into personTable
    values [ person1; person2 ]
} |> conn.InsertAsync
```

Excluding Fields from the Insert:

```f#
open Dapper.FSharp.LinqBuilders
open Dapper.FSharp.MSSQL

let conn : IDbConnection = ... // get it somewhere

let newPerson = { Id = Guid.NewGuid(); FirstName = "Roman"; LastName = "Provaznik"; Position = 1; DateOfBirth = None }

let personTable = table<Person>

insert {
    for p in personTable do
    value newPerson
    exclude p.DateOfBirth
} |> conn.InsertAsync
```

_NOTE: You can exclude multiple fields by using multiple `exclude` statements._

### UPDATE

```F#
let updatedPerson = { existingPerson with LastName = "Vorezprut" }

update {
    for p in personTable do
    set updatedPerson
    where (p.Id = updatedPerson.Id)
} |> conn.UpdateAsync
```

Partial updates are possible by manually specifying one or more `column` properties:

```F#
update {
    for p in personTable do
    set modifiedPerson
    column p.FirstName
    column p.LastName
    where (p.Position = 1)
} |> conn.UpdateAsync
```


Partial updates are also possible by using an anonymous record:

```F#
update {
    for p in personTable do
    set {| FirstName = "UPDATED"; LastName = "UPDATED" |}
    where (p.Position = 1)
} |> conn.UpdateAsync
```

### DELETE

```F#
delete {
    for p in personTable do
    where (p.Position = 10)
} |> conn.DeleteAsync
```

And if you really want to delete the whole table, you must use the `deleteAll` keyword:

```F#
delete {
    for p in personTable do
    deleteAll
} |> conn.DeleteAsync
```

### SELECT

To select all records in a table, you must use the `selectAll` keyword:

```F#
select {
    for p in personTable do
    selectAll
} |> conn.SelectAsync<Person>
```

NOTE: You also need to use `selectAll` if you have a no `where` and no `orderBy` clauses because a query cannot consist of only `for` or `join` statements.

Filtering with where statement:

```F#
select {
    for p in personTable do
    where (p.Position > 5 && p.Position < 10)
} |> conn.SelectAsync<Person>
```

To flip boolean logic in `where` condition, use `not` operator (unary NOT):

```F#
select {
    for p in personTable do
    where (not (p.Position > 5 && p.Position < 10))
} |> conn.SelectAsync<Person>
```

NOTE: The forward pipe `|>` operator in you query expressions because it's not implemented, so don't do it (unless you like exceptions)!

To use LIKE operator in `where` condition, use `like`:
```F#
select {
    for p in personTable do
    where (like p.FirstName "%partofname%")
} |> conn.SelectAsync<Person>
```

Sorting:

```F#
select {
    for p in personTable do
    where (p.Position > 5 && p.Position < 10)
    orderBy p.Position
    thenByDescending p.LastName
} |> conn.SelectAsync<Person>
```

If you need to skip some values or take only subset of results, use skip, take and skipTake. Keep in mind that for correct paging, you need to order results as well.

```F#
select {
    for p in personTable do
    where (p.Position > 5 && p.Position < 10)
    orderBy p.Position
    skipTake 2 3 // skip first 2 rows, take next 3
} |> conn.SelectAsync<Person>
```

#### Option Types and Nulls

Checking for null on an Option type:
```F#
select {
    for p in personTable do
    where (p.DateOfBirth = None)
    orderBy p.Position
} |> conn.SelectAsync<Person>
```

Checking for null on a nullable type:
```F#
select {
    for p in personTable do
    where (p.LastName = null)
    orderBy p.Position
} |> conn.SelectAsync<Person>
```

Checking for null (works for any type):
```F#
select {
    for p in personTable do
    where (isNullValue p.LastName && isNotNullValue p.FirstName)
    orderBy p.Position
} |> conn.SelectAsync<Person>
```

Comparing an Option Type

```F#
let dob = DateTime.Today

select {
    for p in personTable do
    where (p.DateOfBirth = Some dob)
    orderBy p.Position
} |> conn.SelectAsync<Person>
```


### JOINS

For simple queries with join, you can use innerJoin and leftJoin in combination with SelectAsync overload:


```F#

let personTable = table<Person>
let dogsTable = table<Dog>
let dogsWeightsTable = table<DogsWeight>

select {
    for p in personTable do
    join d in dogsTable on (p.Id = d.OwnerId)
    orderBy p.Position
} |> conn.SelectAsync<Person, Dog>
```

`Dapper.FSharp` will map each joined table into separate record and return it as list of `'a * 'b` tuples. Currently up to 2 joins are supported, so you can also join another table here:

```F#
select {
    for p in personTable do
    join d in dogsTable on (p.Id = d.OwnerId)
    join dw in dogsWeightsTable on (d.Nickname = dw.DogNickname)
    orderBy p.Position
} |> conn.SelectAsync<Person, Dog, DogsWeight>
```

The problem with LEFT JOIN is that tables "on the right side" can be full of null values. Luckily we can use SelectAsyncOption to map joined values to Option types:

```F#
// this will return seq<(Person * Dog option * DogWeight option)>
select {
    for p in personTable do
    leftJoin d in dogsTable on (p.Id = d.OwnerId)
    leftJoin dw in dogsWeightsTable on (d.Nickname = dw.DogNickname)
    orderBy p.Position
} |> conn.SelectAsyncOption<Person, Dog, DogsWeight>
```
