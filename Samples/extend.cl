msg is prompt("Enter a message: ")
spaces is i32(prompt("How many spaces? "))

# Our final message
finalMsg is ""
repeat for len(msg) times as i
    finalMsg += elementAt(msg, i)
    count is sub(spaces, 1)
    label REPLABEL
    finalMsg += " "
    loop REPLABEL count
endrep

log(finalMsg)
