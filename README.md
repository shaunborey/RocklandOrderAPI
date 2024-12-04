# RocklandOrderAPI

This repository contains the codebase for the back-end API of the demo Rockland Ordering Application that I developed in response to the following prompt from a potential employer:

- We have an upcoming project that requires a change in our system that handles ordering. We need a button added to this system, which allows a user to upload a purchase order (a pdf). The user needs to upload a purchase order to be able to finish the order. PDF should also be viewable.

-  Programming with (or a combination of) C#/.net/html/JavaScript(any JS framework), provide a small demo of how this might work. Provide working code to us in any way you feel comfortable, so we can run and view the code. You can go as simple, or as big as you want.

**To run the full application, follow these steps to configure the components:**

1. Clone the `RocklandOrderAPI` repository, open the project in Visual Studio, and perform these steps:
    - In the `appsettings.json` file, modify the "DefaultConnection" SQL connection string as needed to point to a SQL Server instance
    - Build the solution and ensure all packages are properly installed
    - Using the Package Manager Console, run the command `Update-Database` which will cause Entity Framework to create the database and objects using the provided connection string.

2. Download the `DataScript.sql` file from the `Rockland-App-Supporting-Files` repository and execute it on the database to insert the required static data.
3. Clone the `rockland-order-app` repository and open the project folder in Visual Studio Code.
4. In Visual Studio, start the RocklandOrderAPI application.
5. In Visual Studio Code, open a terminal and run the command `npm run start` to start the React application
6. Use the `Register` feature in the application to create a user, then you can add some of the products and go through the checkout process.
