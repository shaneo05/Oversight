pragma solidity >=0.4.24 <0.6.0;

//A simple lottery contract designed for a maximum of 30 users.

contract LotteryUsers_30 {
    address[30] totalNumberOfParticipants;
    uint totalNumberOfParticipantsCount = 0;
    uint randNumber = 0;
 

    //Function to check whether a user is already in the game.
    function alreadyInGame(address tempParticipant) private view returns(bool) {
        bool containsParticipant = false;
        for(int index = 0; index < 30; index++) {
            if (totalNumberOfParticipants[index] == tempParticipant) {
                containsParticipant = true;
            }
        }
        return containsParticipant;
    }

    //Function to participate in the game.
    function joinGame() public payable {
        require(msg.value == 0.05 ether, "You must deposit 0.05 ether to join this lottery");
        require(totalNumberOfParticipantsCount < 30, "Capacity for this game has been reached");
        require(alreadyInGame(msg.sender) == false, "User already joined");
        
        totalNumberOfParticipants[totalNumberOfParticipantsCount] = msg.sender;
        totalNumberOfParticipantsCount++;
        if (totalNumberOfParticipantsCount == 30) {
            generateWinner();
        }
    }  
    

    //Function to determine a winner.
    function generateWinner() private returns(address) {
        require(totalNumberOfParticipantsCount == 30, "Free slots still available");
        address payable winner = address(uint160(totalNumberOfParticipants[randomNumber()]));
        winner.transfer(address(this).balance);
        delete totalNumberOfParticipants;
        totalNumberOfParticipantsCount = 0;
        return winner;
    }
    
    function randomNumber() private returns(uint) {
        uint rand = uint(keccak256(abi.encodePacked(now, msg.sender, randNumber))) % 30;
        randNumber;
        return rand;
    }
        
}