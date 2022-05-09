argStr is ''
repeat for len(args) times as i
    argStr += elementAt(args, i)
    if i is not sub(len(args), 1)
        argStr += ", "
    endif
endrep

log("There are {len(args)} arguments that have been provided to this script: {argStr}")

addArgs is prompt("Would you like to add more to this list? [Y/n] ")
if lower(addArgs) is "y"
    newArgs is prompt("Enter arguments to add, separated by a comma.\n> ")
    splitNewArgs is split(newArgs, ",")
    repeat for len(splitNewArgs) times as i
        addElement(args, elementAt(splitNewArgs, i))
    endrep
endif

log("Your final argument list is: {str(args)}")
