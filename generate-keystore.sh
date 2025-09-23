#!/bin/bash

# Generate Android Keystore for CoralClient
# This script helps create the keystore needed for signing your Android app

echo "🔐 CoralClient Android Keystore Generator"
echo "=========================================="
echo ""
echo "This will create a keystore file for signing your Android app."
echo "⚠️  IMPORTANT: Save the passwords securely - you'll need them for every app update!"
echo ""

# Set keystore details
KEYSTORE_FILE="coralclient-release-key.jks"
KEY_ALIAS="coralclient"
VALIDITY_DAYS=10000  # About 27 years
KEY_SIZE=2048

echo "📋 Keystore Configuration:"
echo "   File: $KEYSTORE_FILE"
echo "   Alias: $KEY_ALIAS"
echo "   Validity: $VALIDITY_DAYS days (~27 years)"
echo "   Key Size: $KEY_SIZE bits"
echo ""

# Check if keystore already exists
if [ -f "$KEYSTORE_FILE" ]; then
    echo "❌ Keystore file '$KEYSTORE_FILE' already exists!"
    echo "   If you want to create a new one, please:"
    echo "   1. Back up the existing keystore securely"
    echo "   2. Remove or rename the existing file"
    echo "   3. Run this script again"
    exit 1
fi

# Check if keytool is available
if ! command -v keytool &> /dev/null; then
    echo "❌ keytool not found!"
    echo "   Please install Java Development Kit (JDK) and ensure keytool is in your PATH"
    echo "   On Ubuntu/Debian: sudo apt install openjdk-17-jdk"
    echo "   On macOS: brew install openjdk@17"
    exit 1
fi

echo "🔄 Generating keystore..."
echo ""

# Generate the keystore
keytool -genkey -v \
    -keystore "$KEYSTORE_FILE" \
    -keyalg RSA \
    -keysize $KEY_SIZE \
    -validity $VALIDITY_DAYS \
    -alias "$KEY_ALIAS"

# Check if generation was successful
if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Keystore generated successfully!"
    echo ""
    echo "📝 Next Steps:"
    echo "1. 🔒 SECURELY BACKUP this keystore file: $KEYSTORE_FILE"
    echo "2. 🔑 Remember your passwords (store them in a password manager)"
    echo "3. 📱 Convert to base64 for GitHub Secrets:"
    echo "   base64 -w 0 $KEYSTORE_FILE"
    echo ""
    echo "4. 🚀 Set up GitHub Secrets in your repository:"
    echo "   - ANDROID_KEYSTORE_BASE64: (the base64 output from step 3)"
    echo "   - ANDROID_KEY_ALIAS: $KEY_ALIAS"
    echo "   - ANDROID_KEY_PASSWORD: (the key password you just entered)"
    echo "   - ANDROID_KEYSTORE_PASSWORD: (the keystore password you just entered)"
    echo "   - APP_VERSION: 1.0"
    echo ""
    echo "⚠️  CRITICAL: Never lose this keystore or its passwords!"
    echo "   Google Play requires the same key for all app updates."
else
    echo ""
    echo "❌ Keystore generation failed!"
    echo "   Please check the errors above and try again."
fi