#!/bin/bash

# Convert Android Keystore to Base64 for GitHub Secrets
# This script helps prepare your keystore for GitHub Actions

echo "🔄 CoralClient Keystore to Base64 Converter"
echo "============================================"
echo ""

KEYSTORE_FILE="coralclient-release-key.jks"

# Check if keystore exists
if [ ! -f "$KEYSTORE_FILE" ]; then
    echo "❌ Keystore file '$KEYSTORE_FILE' not found!"
    echo "   Please make sure you have generated the keystore first."
    echo "   Run: ./generate-keystore.sh"
    exit 1
fi

echo "📁 Found keystore: $KEYSTORE_FILE"
echo ""
echo "🔄 Converting to base64..."

# Convert to base64 (single line, no wrapping)
base64_output=$(base64 -w 0 "$KEYSTORE_FILE")

echo ""
echo "✅ Conversion complete!"
echo ""
echo "📋 Copy this base64 string for GitHub Secret 'ANDROID_KEYSTORE_BASE64':"
echo "=========================================================================="
echo "$base64_output"
echo "=========================================================================="
echo ""
echo "📝 GitHub Secrets Setup:"
echo "1. Go to your GitHub repository"
echo "2. Navigate to Settings → Secrets and variables → Actions"
echo "3. Add these repository secrets:"
echo "   - ANDROID_KEYSTORE_BASE64: (paste the base64 string above)"
echo "   - ANDROID_KEY_ALIAS: coralclient"
echo "   - ANDROID_KEY_PASSWORD: (your key password)"
echo "   - ANDROID_KEYSTORE_PASSWORD: (your keystore password)"
echo "   - APP_VERSION: 1.0"
echo ""
echo "💾 The base64 string has been copied to your clipboard (if xclip is available)"

# Try to copy to clipboard
if command -v xclip &> /dev/null; then
    echo "$base64_output" | xclip -selection clipboard
    echo "✅ Copied to clipboard!"
elif command -v pbcopy &> /dev/null; then
    echo "$base64_output" | pbcopy
    echo "✅ Copied to clipboard!"
else
    echo "ℹ️  Install xclip (Linux) or use pbcopy (macOS) for automatic clipboard copying"
fi