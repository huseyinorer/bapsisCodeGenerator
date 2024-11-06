# Bapsis Code Generator

## Overview
Bapsis Code Generator is a tool designed to streamline the development process by automatically generating necessary code files and structures from basic entity classes.

## Features
- Automatic code generation from entity classes
- Creates standardized folder structures
- Generates necessary supporting files
- Streamlines development workflow

## Installation
Clone the repository and ensure you have all required dependencies installed.

## How to Use

### 1. Create Entity Class
Create your basic entity class in the following path:
```
Bapsis.Api.Domain/AggregateRoots/<YourEntityName>s/<YourEntityName>.cs
```

### 2. Run Code Generator
Execute the Bapsis code generator and provide the full path to your created entity. For example:
```
E:\Code\BAPSISLER\bapsis-v2\src\Services\Bapsis.Api\Bapsis.Api.Domain\AggregateRoots\<YourEntityName>s\<YourEntityName>.cs
```

### 3. Follow the Prompts
- Answer the questions prompted by the generator
- Provide any additional required information
- Confirm your choices when prompted

### 4. Verify Generation
After completion:
- Check the solution explorer for newly created folders and files
- Verify that all necessary files have been generated
- Ensure the generated code meets your requirements

## Directory Structure
```
Bapsis.Api.Domain/
├── AggregateRoots/
│   └── <YourEntityName>s/
│       └── <YourEntityName>.cs
└── ...
```

## Important Notes
- Make sure to follow the naming conventions
- Entity names should be in singular form
- The generator will create folders with plural names
- Review generated code before implementing

## Support
For any issues or questions, please file an issue in the repository.

## License
This project is licensed under the MIT License - see the LICENSE file for details.
