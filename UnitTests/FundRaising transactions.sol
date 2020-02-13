pragma solidity >=0.4.24 <0.6.0;

contract FundRaising {
    address payable owner;
    uint public goal;
    uint public endTime = 12000;
    uint public total = 0;
    
    uint public timeLimit = 11000;

    mapping(address=>uint) donations;
    
    constructor(uint _goal) public {

        assert(endTime >= timeLimit);

        owner = msg.sender;
        goal = _goal;
        endTime = now + timeLimit;
    }
    
    function add() payable public {
        require(now < endTime, "Fundraising is closed.");
        require(total < goal, "We reached a goal.");
        require(msg.value > 0, "You need to send some ether");
        donations[msg.sender] += msg.value; 
        total += msg.value;
    }
    
    function withdrawOwner() public {
        require(msg.sender == owner, "You must be owner");
        require(total >= goal, "Fundraising not closed yet");
        owner.transfer(address(this).balance);
    }
    
    function withdraw() public {
        require(now > endTime, "Fundraising not closed");
        require(total < goal, "Can not withdraw when fundraising was successful");
        uint amount = donations[msg.sender];
        total -= amount;
        donations[msg.sender] = 0;
        address(msg.sender).transfer(amount);
    }
}