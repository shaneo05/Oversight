pragma solidity >=0.4.24<0.6.0;
import "C:/Users/shane/Desktop/TempOversight/UnitTests/ValidationSpecs.sol";

contract LoopFor {

    int x;
    int y;

    function ContractInvariant () private view {
        OverSight.ContractInvariant(x == y);
        OverSight.ContractInvariant(y >= 0);
    }

    // test Loop invariant with for loop
    constructor(int n) public {
        require (n >= 0);
        x = n;
        y = x;
    }

    function Foo() public {
        if ( x > 0 ) 
        {
           x--;
           y--;
        }
        assert (y < 0); 
    } 
}
