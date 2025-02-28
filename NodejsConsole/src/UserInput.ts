import { createInterface } from "readline";
var Writable = require('stream').Writable;

//------------------------------------------------------------------------------
// A little helper for interacting with the user
//------------------------------------------------------------------------------
export class UserInput
{
    hideTypeing: Boolean;

    //------------------------------------------------------------------------------
    // ctor
    //------------------------------------------------------------------------------
    constructor(hideTypeing: Boolean = false)
    {
        this.hideTypeing = hideTypeing;   
    }

    //------------------------------------------------------------------------------
    // Ask the user something
    //------------------------------------------------------------------------------
    async getUserInput(query: string): Promise<string> {
        let readLineInterface = createInterface({
            input: process.stdin,
            output: new Writable({write: (chunk: Buffer, encoding: any, callback: any) =>
                {
                    if(this.hideTypeing && chunk.toString().startsWith(query)){
                        chunk = Buffer.from(query +  "*".repeat(chunk.length - query.length))
                    }
                    else if(this.hideTypeing && chunk.length == 1)
                    {
                        chunk = Buffer.from("*".repeat(chunk.length))
                    }
                    process.stdout.write(chunk, encoding);
                    callback();
                }}),
            terminal: true
        });         
              
        return new Promise((resolve) => {
                readLineInterface.question(query, (input) => resolve(input) );
            }).then(value => {
                readLineInterface.close();
                return value as string 
            });


    }
}



