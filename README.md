# Cubelang
Cubelang! The bodgiest language around.

```
log("Hello world!")
```

Primarily designed for the [roll](https://rollbot.net) discord bot as its scripting language, however this repo includes a desktop runtime you can use to run Cubelang applications on the desktop via a command line.

WIP! **Yes, the interpreter is bad.** But it works. I will probably redo it to a proper interpreter at some point, though!

### Running code
In order to run code, clone this repo locally, and compile `Cubelang.Runtime`.

Then, to run your code, just execute `./Cubelang.Runtime(.exe if on windows) <script filename> <any arguments>`