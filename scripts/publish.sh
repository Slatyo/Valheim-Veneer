#!/bin/bash

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --target) TARGET="$2"; shift 2 ;;
        --target-path) TARGET_PATH="$2"; shift 2 ;;
        --target-assembly) TARGET_ASSEMBLY="$2"; shift 2 ;;
        --valheim-path) VALHEIM_PATH="$2"; shift 2 ;;
        --deploy-path) DEPLOY_PATH="$2"; shift 2 ;;
        --project-path) PROJECT_PATH="$2"; shift 2 ;;
        *) shift ;;
    esac
done

# Plugin name without ".dll"
NAME="${TARGET_ASSEMBLY%.dll}"

echo "Publishing for $TARGET from $TARGET_PATH"

if [ "$TARGET" == "Debug" ]; then
    if [ -z "$DEPLOY_PATH" ]; then
        DEPLOY_PATH="$VALHEIM_PATH/BepInEx/plugins"
    fi

    PLUG="$DEPLOY_PATH/$NAME"
    mkdir -p "$PLUG"
    echo "Copy $TARGET_ASSEMBLY to $PLUG"
    cp "$TARGET_PATH/$NAME.dll" "$PLUG/"
    [ -f "$TARGET_PATH/$NAME.pdb" ] && cp "$TARGET_PATH/$NAME.pdb" "$PLUG/"
    [ -f "$TARGET_PATH/$NAME.xml" ] && cp "$TARGET_PATH/$NAME.xml" "$PLUG/"
fi

if [ "$TARGET" == "Release" ]; then
    echo "Packaging for ThunderStore..."
    PACKAGE_PATH="$PROJECT_PATH/Package"

    mkdir -p "$PACKAGE_PATH/plugins"
    cp "$TARGET_PATH/$TARGET_ASSEMBLY" "$PACKAGE_PATH/plugins/"
    [ -f "$PROJECT_PATH/README.md" ] && cp "$PROJECT_PATH/README.md" "$PACKAGE_PATH/"

    cd "$PACKAGE_PATH"
    zip -r "$TARGET_PATH/$TARGET_ASSEMBLY.zip" ./*
fi
