NULL =

bin_SCRIPTS = \
	jflabbergast \
	jflabbergast-repl \
	$(NULL)

man1_MANS = \
	jflabbergast.1 \
	jflabbergast-repl.1 \
	$(NULL)

compilerdir = $(pkgdatadir)/compiler
compiler_SCRIPTS = jvm-build-cache

javadir = $(datadir)/java
java_DATA = \
	flabbergast-runtime-$(VERSION).jar \
	flabbergast-compiler-$(VERSION).jar \
	$(NULL)

JAVA_RUNTIME_DEPS = \
	$(JLINE_JAR) \
	$(NULL)

JAVA_COMPILER_DEPS = \
	$(ASM_JAR) \
	$(COMMONS_CLI_JAR) \
	$(NULL)

RUNTIME_INPUTS = $(wildcard runtime/flabbergast/*.java)
COMPILER_INPUTS = $(wildcard compiler/flabbergast/*.java)

SPACE = $(NULL) $(NULL)

flabbergast-runtime-$(VERSION).jar: runtime.manifest $(RUNTIME_INPUTS)
	CLASSPATH=$(subst $(SPACE),:,$(JAVA_RUNTIME_DEPS)) ./jarc runtime $(VERSION)

flabbergast-compiler-$(VERSION).jar: compiler.manifest $(COMPILER_INPUTS) flabbergast-runtime-$(VERSION).jar
	CLASSPATH=$(subst $(SPACE),:,$(JAVA_COMPILER_DEPS) $(JAVA_RUNTIME_DEPS)):flabbergast-runtime-$(VERSION).jar ./jarc compiler $(VERSION)

runtime.manifest: Makefile
	echo "Manifest-Version: 1.0\nClass-Path: $(addprefix $(javadir), $(JAVA_RUNTIME_DEPS))\nCreated-By: $(VERSION) (The Flabbergast Project)" > $@

compiler.manifest: Makefile
	echo "Manifest-Version: 1.0\nClass-Path: $(addprefix $(javadir), $(JAVA_COMPILER_DEPS) flabbergast-runtime-$(VERSION).jar)\nCreated-By: $(VERSION) (The Flabbergast Project)" > $@

jvm-build-cache: Makefile
	echo "#!/bin/sh\ncd \"$$1\"\nexec $(JAVA) -cp $(subst $(SPACE),:,$(JAVA_COMPILER_DEPS) $(JAVA_RUNTIME_DEPS)):$(javadir)flabbergast-runtime-$(VERSION).jar:$(javadir)flabbergast-compiler-$(VERSION).jar flabbergast.MainBuildCache" > $@

jflabbergast: Makefile
	echo "#!/bin/sh\nexec $(JAVA) -cp $(subst $(SPACE),:,$(JAVA_COMPILER_DEPS) $(JAVA_RUNTIME_DEPS)):$(javadir)flabbergast-runtime-$(VERSION).jar:$(javadir)flabbergast-compiler-$(VERSION).jar flabbergast.MainPrinter \"$$@\"" > $@

jflabbergast-repl: Makefile
	echo "#!/bin/sh\nexec $(JAVA) -cp $(subst $(SPACE),:,$(JAVA_COMPILER_DEPS) $(JAVA_RUNTIME_DEPS)):$(javadir)flabbergast-runtime-$(VERSION).jar:$(javadir)flabbergast-compiler-$(VERSION).jar flabbergast.MainREPL \"$$@\"" > $@

install-data-hook:
	$(LN_S) -f flabbergast-runtime-$(VERSION).jar $(DESTDIR)$(datadir)/java/flabbergast-runtime.jar
	$(LN_S) -f flabbergast-compiler-$(VERSION).jar $(DESTDIR)$(datadir)/java/flabbergast-compiler.jar

CLEANFILES = \
	jvm-build-cache \
	jflabbergast \
	jflabbergast-repl \
	$(NULL)