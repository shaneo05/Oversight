# Oversight (Development)
A Windows based Verification and Validation tool for Smart Contracts

Oversight is a formal verification tool for Smart Contracts written in Solidity that should be able to produce both proofs and counter examples. 
The tool will aim to perform verification/validation on a given smart contract and once a functioning prototype of the tool exists, it will be tested in a controlled environment by using techniques such as assertion testing using example solidity code. 
Through this process, the tools functionality can be validated with further efforts having been made to implement inference using corral, if time permits so. The further goal to locate bugs/semantic errors within the source code as well as formal verification but without the need for external explicit modifications provided by the user in the test code i.e. (assertion testing).

Unless stated otherwise, branch "master" will contain the release code.


# Installation

This zip repository contains all required dependencies/code pre-installed, thus leaving you to complete the following below.

Download the zip (development) to your local machine.
Place the folder in your "Main Drive" (C:/) (H:/)

If a different source location is specified other than (C:/) then the location of the OverSight repo
must also be amended within the batch file (runOverSight.bat).

The batch file can be found in Sources\OverSight

After adding the location of the "bat" file to "PATH" (refer to environment variables) the application can then be invoked from the CMD.

To Run the application type "runOverSight"

With 0 arguements the application will display usage details

With 2 arguements (Location/Name of sol contract) (contract class name) the application will begin the conversion process and attempt verification in the contracts Boogie representation.

VerificationOutcome.txt will then hold the outcome of this verification attempt, stating what caused it to either fail or pass and how many conditions were verified.

This then in purpose provides layer of verfication prior to submission of a sol contract onto the blockchain.


# Examples

If any doubt arises regarding running the program, the following section should provide sufficient clarification using screenshots.

# Licensing

Oversight will be compliant as per the requirements of the MIT license.

# Developers

Shane Gill (Active Developer)
