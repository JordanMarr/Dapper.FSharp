﻿module Dapper.FSharp.Tests.SelectExpressionTests

open System.Threading.Tasks
open Dapper.FSharp
open Dapper.FSharp.Tests.Database
open Expecto
open FSharp.Control.Tasks.V2
open ExpressionBuilders

type Person = {
    FName: string
    LName: string
    Age: int
}

let testsBasic() = testList "SELECT EXPRESSION" [
    
    testTask "Simple Query" {
        let query = 
            select {
                for p in entity<Person> do
                where (p.FName = "John")
                orderBy p.LName
            }

        Expect.equal query.Table "Person" "Expected table = 'Person'"
        Expect.equal query.Where (eq "FName" "John") "Expected FName = 'John'"
        Expect.equal query.OrderBy [("LName", Asc)] "Expected Order By 'LName'"
    }

    testTask "Complex Query" {
        let query = 
            select {
                for p in entity<Person> do
                where (p.FName = "John" && p.LName = "Doe")
                orderByDescending p.LName
                orderByDescending p.Age
            }
    
        Expect.equal query.Table "Person" "Expected table = 'Person'"
        Expect.equal query.Where (eq "FName" "John" + eq "LName" "Doe") "Expected FName = 'John' && LName = 'Doe'"
        Expect.equal query.OrderBy [("LName", Desc); ("Age", Desc)] "Expected Order By 'LName DESC, Age DESC'"
    }

    testTask "Unary Not" {
        let query = 
            select {
                for p in entity<Person> do
                where (not (p.FName = "John"))
            }
    
        Expect.equal query.Where (!!(eq "FName" "John")) "Expected not (FName = 'John')"
    }

    testTask "Group By" {
        let query = 
            select {
                for p in entity<Person> do
                where (not (p.FName = "John"))
                groupBy p.Age
            }
    
        Expect.equal query.GroupBy ["Age"] "Expected group by 'Age'"
    }

]
