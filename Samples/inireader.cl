# inireader.cl - Read .ini files into a dictionary, then output the dictionary.

# There must be at least 1 argument provided - all others are ignored.
if len(args) is lthan 1
    log("No .ini file has been provided.")
    stop
# Check to see if file exists.
else if not file_exists(args[0])
    log("File \"{args[0]}\" does not exist.")
    stop
endif

# Read the file into a string.
file is file_read(args[0])

# Split each line of the string into a list.
lines is split(file, "\n")

# Create our dictionary which will contain the read ini data.
ini is Dict()
currentSect is ""
repeat for len(lines) times as i
    line is trim(lines[i])
    # Ignore blank lines and comments
    if line is ""
        continue
    else if startsWith(line, ";")
        continue
    # If the line starts with a '[' we know it is a section
    else if startsWith(line, "[")
        # Create our section, removing the [] and removing any whitespace.
        currentSect is trim(range(line, 1, sub(len(line), 1)))
        # Add it to our dictionary as a new dictionary.
        ini[currentSect] is Dict()
    else
        # Sort out our sections...
        splitLine is split(line, "=")
        # All this does is get the dictionary for the current section, then splits the line into
        # a list separated by the equals. It then sets the first value as the key, and joins the
        # rest by the equals sign to get the value. Simple!
        ini[currentSect][trim(splitLine[0])] is trim(join(range(splitLine, 1, len(splitLine)), "="))
    endif
endrep

# Output the dictionary
log(ini)
