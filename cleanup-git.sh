#!/bin/bash

echo "🧹 Git Repository Cleanup Script"
echo "================================"
echo ""

# Remove any accidentally tracked files
echo "🔍 Checking for accidentally tracked files..."

# List files that should be ignored but might be tracked
TRACKED_IGNORED=$(git ls-files | grep -E "(bin/|obj/|\.db$|\.db-|\.DS_Store|motion\.db)" || true)

if [ ! -z "$TRACKED_IGNORED" ]; then
    echo "⚠️ Found tracked files that should be ignored:"
    echo "$TRACKED_IGNORED"
    echo ""
    echo "🗑️ Removing from git tracking..."
    echo "$TRACKED_IGNORED" | xargs git rm --cached
    echo ""
    echo "✅ Files removed from tracking (but kept on disk)"
else
    echo "✅ No accidentally tracked files found!"
fi

echo ""
echo "📊 Current repository status:"
git status --short

echo ""
echo "🎯 Files properly ignored:"
echo "   ✅ /homemonitor-combined (entire folder)"
echo "   ✅ bin/ and obj/ directories"
echo "   ✅ *.db database files"
echo "   ✅ .DS_Store system files"
echo "   ✅ IDE configuration files"
echo ""
echo "📂 Files ready for commit:"
echo "   📁 backend/ (source code only)"
echo "   📁 homemonitor/ (source code only)"
echo "   📁 esp32-arduino/ (Arduino sketch)"
echo "   📄 Configuration files"
echo "   📖 Documentation"
echo ""
echo "🚀 Ready for: git add . && git commit -m 'Initial commit'"