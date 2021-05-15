﻿module Dapper.FSharp.Tests.SelectExpressionTests

open System.Threading.Tasks
open Dapper.FSharp
open Dapper.FSharp.Tests.Database
open Expecto
open FSharp.Control.Tasks.V2
open ExpressionBuilders

type Person = {
    Id: int
    FName: string
    MI: string option
    LName: string
    Age: int
}

type Address = {
    PersonId: int
    City: string
    State: string
}

type Contact = {
    PersonId: int
    Phone: string
}

let testsBasic() = testList "SELECT EXPRESSION" [
    
    testTask "Simple Where" {
        let query = 
            select {
                for p in entity<Person> do
                where (p.FName = "John")
                orderBy p.LName
            }

        Expect.equal query.Table "Person" "Expected table = 'Person'"
        Expect.equal query.Where (eq "Person.FName" "John") "Expected WHERE Person.FName = 'John'"
        Expect.equal query.OrderBy [("Person.LName", Asc)] "Expected ORDER BY Person.LName"
    }

    testTask "Binary Where" {
        let query = 
            select {
                for p in entity<Person> do
                where (p.FName = "John" && p.LName = "Doe")
                orderByDescending p.LName
                orderByDescending p.Age
            }
    
        Expect.equal query.Table "Person" "Expected table = 'Person'"
        Expect.equal query.Where (eq "Person.FName" "John" + eq "Person.LName" "Doe") "Expected WHERE Person.FName = 'John' && Person.LName = 'Doe'"
        Expect.equal query.OrderBy [("Person.LName", Desc); ("Person.Age", Desc)] "Expected ORDER BY 'Person.LName DESC, Person.Age DESC'"
    }

    testTask "Unary Not" {
        let query = 
            select {
                for p in entity<Person> do
                where (not (p.FName = "John"))
            }
    
        Expect.equal query.Where (!!(eq "Person.FName" "John")) "Expected NOT (Person.FName = 'John')"
    }

    testTask "Group By" {
        let query = 
            select {
                for p in entity<Person> do
                count "*" "Count"
                where (not (p.FName = "John"))
                groupBy p.Age
            }
    
        Expect.equal query.GroupBy ["Person.Age"] "Expected GROUP BY Person.Age"
        Expect.equal query.Aggregates [Count ("*", "Count")] "Expected COUNT(*) as [Count]"
    }

    testTask "Optional Column is None" {
        let query = 
            select {
                for p in entity<Person> do
                where (p.MI = None)
            }
    
        Expect.equal query.Where (isNullValue "Person.MI") "Expected Person.MI IS NULL"
    }

    testTask "Optional Column is not None" {
        let query = 
            select {
                for p in entity<Person> do
                where (p.MI <> None)
            }
    
        Expect.equal query.Where (isNotNullValue "Person.MI") "Expected MI IS NOT NULL"
    }

    testTask "Optional Column = Some value" {
        let query = 
            select {
                for p in entity<Person> do
                where (p.MI = Some "N")
            }
    
        Expect.equal query.Where (eq "Person.MI" "N") "Expected Person.MI = 'N'"
    }

    testTask "SqlMethods.isIn" {
        let query = 
            select {
                for p in entity<Person> do
                where (isIn p.Age [18;21])
            }
    
        Expect.equal query.Where (Column ("Person.Age", In [18;21])) "Expected Person.Age IN (18,21)"
    }

    testTask "SqlMethods.isNotIn" {
        let ages = [1..5]
        let query = 
            select {
                for p in entity<Person> do
                where (isNotIn p.Age ages)
            }
    
        Expect.equal query.Where (Column ("Person.Age", NotIn [1;2;3;4;5])) "Expected Person.Age NOT IN (1,2,3,4,5)"
    }

    testTask "Like" {
        let query = 
            select {
                for p in entity<Person> do
                where (like p.LName "D%")
            }
    
        Expect.equal query.Where (Column ("Person.LName", Like "D%")) "Expected LName Person.LIKE \"D%\""
    }

    testTask "Count" {
        let query = 
            select {
                for p in entity<Person> do
                count "*" "Count"
                where (not (p.FName = "John"))
            }
    
        Expect.equal query.Aggregates [Count ("*", "Count")] "Expected count(*) as [Count]"
    }
    
    testTask "Max By" {
        let query = 
            select {
                for p in entity do
                maxBy p.Age
            }
    
        Expect.equal query.Aggregates [Max ("Person.Age", "Person.Age")] "Expected max(Age) as [Age]"
    }
    
    testTask "Join" {
        let query = 
            select {
                for p in entity<Person> do
                join a in entity<Address> on (p.Id = a.PersonId) 
                where (p.Id = a.PersonId) 
            }
    
        Expect.equal query.Joins [InnerJoin ("Address", "PersonId", "Person.Id")] "Expected INNER JOIN Address ON Person.Id = Address.PersonId"
        Expect.equal query.Where (Column ("Person.Id", Eq "Address.PersonId")) "Expected Person.Id = Address.PersonId"
    }
    
    testTask "Join2" {
        let query = 
            select {
                for p in entity<Person> do
                join a in entity<Address> on (p.Id = a.PersonId) 
                join c in entity<Contact> on (p.Id = c.PersonId)
                where (p.Id = a.PersonId && c.Phone = "919-765-4321")
            }
    
        Expect.equal query.Joins [
            InnerJoin ("Address", "PersonId", "Person.Id")
            InnerJoin ("Contact", "PersonId", "Person.Id")
        ] "Expected INNER JOIN Address ON Person.Id = Address.PersonId"
    }

    testTask "LeftJoin" {
        let query = 
            select {
                for p in entity<Person> do
                leftJoin a in entity<Address> on (p.Id = a.PersonId) 
                where (p.Id = a.PersonId) 
            }
    
        Expect.equal query.Joins [LeftJoin ("Address", "PersonId", "Person.Id")] "Expected LEFT JOIN Address ON Person.Id = Address.PersonId"
        Expect.equal query.Where (Column ("Person.Id", Eq "Address.PersonId")) "Expected Person.Id = Address.PersonId"
    }

    testTask "LeftJoin2" {        
        let query = 
            select {
                for p in entity<Person> do
                leftJoin a in entity<Address> on (p.Id = a.PersonId) 
                leftJoin c in entity<Contact> on (p.Id = c.PersonId)
                where (p.Id = a.PersonId && c.Phone = "919-765-4321")
            }
    
        Expect.equal query.Joins [
            LeftJoin ("Address", "PersonId", "Person.Id")
            LeftJoin ("Contact", "PersonId", "Person.Id")
        ] "Expected LEFT JOIN Address ON Person.Id = Address.PersonId"
    }

    testTask "Join2 with Custom Schema and Table Names" {
        let personTable = entity<Person> |> mapSchema "dbo"  |> mapTable "People"
        let addressTable = entity<Address> |> mapSchema "dbo"  |> mapTable "Addresses"
        let contactTable = entity<Contact> |> mapSchema "dbo"  |> mapTable "Contacts"

        let query = 
            select {
                for p in personTable do
                join a in addressTable on (p.Id = a.PersonId) 
                join c in contactTable on (p.Id = c.PersonId)
                where (p.FName = "John" && a.City = "Chicago" && c.Phone = "919-765-4321")
                orderByDescending p.Id
                thenBy a.City
                thenBy c.Phone
            }
    
        Expect.equal query.Schema (Some "dbo") "Expected schema = dbo"
        Expect.equal query.Table "People" "Expected table = People"
        Expect.equal query.Joins [
            InnerJoin ("dbo.Addresses", "PersonId", "dbo.People.Id")
            InnerJoin ("dbo.Contacts", "PersonId", "dbo.People.Id")
        ] "Expected tables and columns to be fully qualified with schema and overriden table names"
        Expect.equal query.Where 
            (eq "dbo.People.FName" "John" + eq "dbo.Addresses.City" "Chicago" + eq "dbo.Contacts.Phone" "919-765-4321") 
            "Expected tables and columns to be fully qualified with schema and overriden table names"
        Expect.equal query.OrderBy [
            OrderBy ("dbo.People.Id", Desc)
            OrderBy ("dbo.Addresses.City", Asc)
            OrderBy ("dbo.Contacts.Phone", Asc)
        ] "Expected tables and columns to be fully qualified with schema and overriden table names"
    }

    testTask "LeftJoin2 with Custom Schema and Table Names" {
        let personTable = entity<Person> |> mapTable "People" |> mapSchema "dbo"
        let addressTable = entity<Address> |> mapTable "Addresses" |> mapSchema "dbo"
        let contactTable = entity<Contact> |> mapTable "Contacts" |> mapSchema "dbo"
                                                                    
        let query = 
            select {
                for p in personTable do
                leftJoin a in addressTable on (p.Id = a.PersonId) 
                leftJoin c in contactTable on (p.Id = c.PersonId)
                where (p.FName = "John" && a.City = "Chicago" && c.Phone = "919-765-4321")
                orderBy p.Id
                thenBy a.City
                thenByDescending c.Phone
            }
    
        Expect.equal query.Schema (Some "dbo") "Expected schema = dbo"
        Expect.equal query.Table "People" "Expected table = People"
        Expect.equal query.Joins [
            LeftJoin ("dbo.Addresses", "PersonId", "dbo.People.Id")
            LeftJoin ("dbo.Contacts", "PersonId", "dbo.People.Id")
        ] "Expected tables and columns to be fully qualified with schema and overriden table names"
        Expect.equal query.Where 
            (eq "dbo.People.FName" "John" + eq "dbo.Addresses.City" "Chicago" + eq "dbo.Contacts.Phone" "919-765-4321") 
            "Expected tables and columns to be fully qualified with schema and overriden table names"
        Expect.equal query.OrderBy [
            OrderBy ("dbo.People.Id", Asc)
            OrderBy ("dbo.Addresses.City", Asc)
            OrderBy ("dbo.Contacts.Phone", Desc)
        ] "Expected tables and columns to be fully qualified with schema and overriden table names"
    }
]
