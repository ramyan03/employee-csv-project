// TypeScript is a superset of JavaScript. It ensures that variables declared as strings can only hold string values.
// TypeScript will throw a compile-time error if you try to assign a non-string value to a string variable.
// TypeScript is a strongly typed language, which means that types are enforced at compile time. It is also statically typed not dynamically typed like JavaScript.
// Main benefits are type safety, improved tooling, maintainability, and better collaboration in large codebases.

// tsc command to compile the TypeScript file to JavaScript
// tsc test.ts

// Type string
const username: string = "Ramyan";
console.log(`Hello, ${username}! Welcome to TypeScript.`);

// Type number
const a: number = 10;
const b: number = 20;
const sum: number = a + b;
console.log(`The sum of ${a} and ${b} is ${sum}.`);

const c: string = "30";
const sumErr = a + c; // This will add 10 + the string "30", resulting in "1030"
console.log(sumErr)

let myName = 'Dave'; // Inferred type string
// myName = 42; // Error: Type 'number' is not assignable to type 'string'
myName = "Ramyan";
console.log(myName);

let isLoading = false; // Inferred type boolean
// isLoading = "yes"; // Error: Type 'string' is not assignable to type 'boolean'
isLoading = true;
console.log(isLoading); 

let album: unknown; // Explicitly declared type unkown
album = 42;
album = "The Dark Side of the Moon";
console.log(album);

const sumNums = (x: number, y: string): number => {
  return x + parseInt(y);
};

console.log(`The sum is: ${sumNums(5, "10")}`); // Outputs: The sum is: 15

let myVar: string | number;
myVar = "Hello";
console.log(`myVar as string: ${myVar}`);
myVar = 100;
console.log(`myVar as number: ${myVar}`);

const re: RegExp = /\w+/g;
console.log(`Regex test on 'Hello World': ${re.test("Hello World")}`);

const StringArray = ["one", "two", "three"];
StringArray.push("four");
// StringArray[0] = 0; // Error: Type 'number' is not assignable to type 'string'
console.log(`StringArray first element contents: ${StringArray[1]}`);
StringArray[1] = "changed";
console.log(`StringArray first element contents: ${StringArray[1]}`);

let mixedArray = ["word", 1, false];
mixedArray = StringArray; // Can accept because it type is (string | number | boolean)[]

console.log(`First element of StringArray: ${StringArray[0]}`);
console.log(`mixedArray contents: ${mixedArray}`);

const mixedTuple: [string, number, boolean] = ["word", 1, false];
// mixedTuple = mixedArray; // Mixed array may not have 3 elements so it can't be assigned to tuple which requires exactly 3 elements
console.log(`Tuple first element: ${mixedTuple[0]}`);

// Objects
const myObj = {
    prop1: "R",
    prop2: 22,
}

myObj.prop1 = "Ramyan";
// myObj.prop2 = "changed"; // Error: Type 'string' is not assignable to type 'number'

type formulaOneDriver = {
    name: string;
    carNumber?: number;
    isWorldChampion: boolean;
}

const driver: formulaOneDriver = {
    name: "Lewis Hamilton",
    // Do not need carNumber as it is optional (?)
    carNumber: 44,
    // Missing isWorldChampion will throw error
    isWorldChampion: true,
}

console.log(driver.name);
// driver.team = "Mercedes"; // Error: Property 'team' does not exist on type 'formulaOneDriver'

interface formulaOneTeam{
    teamName: string;
    base: string;
    championshipsWon?: number;
    drivers?: formulaOneDriver[];
}

const Mercedes: formulaOneTeam = {
    teamName: "Mercedes-AMG Petronas Formula One Team",
    base: "Brackley, United Kingdom",
    championshipsWon: 8,
}

console.log(Mercedes.championshipsWon); 
// console.log(Mercedes.championshipsWon*2); // Possibly undefined


// Type alias
type StringOrNum = string | number;
type StringorNumArray = StringOrNum[];
// Cannot do this with interface
const myStringOrNumArray: StringorNumArray = [1, "two", 3, "four"];
console.log(`StringorNumArray contents: ${myStringOrNumArray[0]}, ${myStringOrNumArray[1]}, ${myStringOrNumArray[2]}, ${myStringOrNumArray[3]}`);


// Literal types
let userNameLiteral: "Admin" | "User" | "Guest";
userNameLiteral = "Admin";
// userNameLiteral = "SuperAdmin"; // Error: Type '"SuperAdmin"' is not assignable to type '"Admin" | "User" | "Guest"'
console.log(`User role is: ${userNameLiteral}`);


// Functions
type MathOperation = (a: number, b: number) => number;
const multiply: MathOperation = (x, y): number => {
    return x * y;
}
console.log(`Multiplication result: ${multiply(5, 10)}`);


// Void function
const logMessage = (message: string): void => {
    console.log(`Log: ${message}`);
}
logMessage("This is a test message.");
logMessage(multiply(2, 3).toString());
logMessage(`${driver.name} drives for ${Mercedes.teamName}`);


// Optional parameters
const addAll = (a: number, b: number, c?: number): number => {
  if (c !== undefined) {
    return a + b + c;
  }
  return a + b;
};
logMessage(addAll(5, 10).toString());

// Rest parameters
const total = (...nums: number[]): number => {
  return nums.reduce((prev, curr) => prev + curr);
}
logMessage(total(1,2,3,4,5).toString());


// Never type -> Error typing
// const throwError = (message: string): never => {
    // throw new Error(message);
// }
// throwError("This is a critical error!");

// Assertion functions
const addorConcat = (a: number, b: number, c: "add" | "concat"): number | string =>{
    if(c === "add"){
        return a + b;
    } else {
        return "" + a + b;
    }
}

const myVal = addorConcat(10, 20, "concat") as string;
const nextVal = addorConcat(10, 20, "add") as number;
console.log(`myVal: ${myVal}, nextVal: ${nextVal}`);


// DOM
// const img = document.querySelector('img')!; // Non null assertion
// const myImg = document.getElementById('#img') as HTMLImageElement; 

// img.src = "https://media.gq-magazine.co.uk/photos/5e662a63b4e1880008727a40/16:9/w_2560%2Cc_limit/20200309-F1-Round-Up-01.jpg";
// myImg.src = "https://cloudfront-us-east-2.images.arcpublishing.com/reuters/DJHPPYRU2VIXXIL3CIN57XB42I.jpg";


// Classes
class Student {
    public name: string;
    public age: number;
    public readonly id: number;
    public apClass?: string;

    constructor(name: string, age: number, id: number) {
        this.name = name;
        this.age = age;
        this.id = id;
    }

    public getAge(): number{
        return this.age;
    }

    private getId(): number{
        return this.id;
    }

    protected getName(): string{
        return this.name;
    }

    // Public is accessible anywhere, Private is only in this class, Protected is in this class and subclasses
}

const bojack = new Student("Bojack Horseman", 45, 101);
console.log(`Student Name: ${bojack.name}, Age: ${bojack.getAge()}`);

// Can extend classes
class ApStudent extends Student {
    public subject: string;

    constructor(name: string, age: number, id: number, subject: string) {
        super(name, age, id);
        this.subject = subject;
        this.apClass = "AP " + subject;
    }

    public getStudentInfo(): string {
        return `Name: ${this.getName()}, Age: ${this.getAge()}, Subject: ${this.subject}`;
    }
}

const diane = new ApStudent("Diane Nguyen", 30, 102, "Literature");
console.log(diane.getStudentInfo());


// Index signatures
interface TransactionObj {
    [index: string]: number; // All the indexes must be of type string and values must be of type number. Object can have any number of properties as long as they follow this rule
}

const todaysTransactions: TransactionObj = {
    Pizza: -10,
    Books: -5,
    Job: 50,
    // Name: "Me" // Error: Type 'string' is not assignable to type 'number'
}
console.log(todaysTransactions.Pizza);

interface Students {
    //[key: string]: string | number | number[] | undefined
    name: string,
    GPA: number,
    classes?: number[] // optional
}

const student: Students = {
    name: "Doug",
    GPA: 3.5,
    classes: [100, 200]
}

for (const key in student) {
    console.log(`${key}: ${student[key as keyof Students]}`) // key as keyof Students means that key is one of the keys defined in students interface. Basically telling TS that we know key is valid
}

Object.keys(student).map(key => {
    console.log(student[key as keyof typeof student]) // Typeof student is more specific if student is declared with extra narrow literals
})

const logStudentKey = (student: Students, key: keyof Students): void => {
    console.log(`Student ${key}: ${student[key]}`)
}

logStudentKey(student, 'name');


// Generics
const isObj = <T>(arg: T): boolean => {
    return (typeof arg === 'object' && !Array.isArray(arg) && arg !== null) // Returns true if object, not an array and not null
}
const isTrue = <T>(arg: T): { arg: T, is: boolean } => {
    if (Array.isArray(arg) && !arg.length) { // Empty array check
        return { arg, is: false }
    }
    if (isObj(arg) && !Object.keys(arg as keyof T).length) {
        return { arg, is: false }
    }
    return { arg, is: !!arg }
}
console.log(isTrue(false))
console.log(isTrue(0))
console.log(isTrue(true))

export {};