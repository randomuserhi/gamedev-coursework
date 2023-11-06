RHU.require(new Error(), { 
    docs: "docs", rhuDocuscript: "docuscript",
}, function({
    docs, rhuDocuscript,
}) {
    docs.jit = (version, path) => docuscript<RHUDocuscript.Language, RHUDocuscript.FuncMap>(({
        h, p, frag, br, link
    }) => {
        frag(
            p(
                "This is the Docuscript document for my Multimedia and Game Development coursework. The Github rep can be found ",
                link("https://github.com/randomuserhi/gamedev-coursework", "here"), "."
            )
        );

        h(1, "test");
    }, rhuDocuscript);
});