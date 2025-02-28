import { UserInput } from "./UserInput"
import { AppOptions } from "./AppOptions";
import chalk from "chalk"

//------------------------------------------------------------------------------
// main
//------------------------------------------------------------------------------
async function main(options: AppOptions) {
    if(options.badArgs.length > 0)
    {
        console.log("ERROR: Bad arguments: ");
        options.badArgs.forEach(arg => console.log(`  ${arg}`));
        process.exit();
    }
    
    // TODO: Init resources here
    var userInput = new UserInput();
    let name = await userInput.getUserInput("What is your name: ");
    userInput.hideTypeing = true;
    let password = await userInput.getUserInput("What is your password: ");
    console.log(chalk.yellowBright(`Hi ${name},`))
    console.log(`Your password is ${password}`)

    
    const cleanup = () =>
    {
        // TODO: Clean up any resources here
    }

    process.on("SIGINT", async () => {
        console.log("*** Process was interrupted! ***")
        cleanup();
        process.exit();
    });

    try {
        // TODO: Run your application code here with your options
        // await myObject.doSomething();
        return 0;
    } catch (error: any) {
        console.error(error.stack);
        return 1;
    } finally {
        cleanup();
        console.log("Bye Bye");
    }
}


//------------------------------------------------------------------------------
//------------------------------------------------------------------------------
//------------------------------------------------------------------------------

console.log('==========================================')
console.log('=  MY COOL APP v0.01.00                  =')
console.log('==========================================')

main(new AppOptions(process.argv))
    .then(status => {
        console.log(`Exiting with status: ${status}`)
        process.exit(status);
    });

