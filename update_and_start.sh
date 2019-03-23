killall Fox
wget https://ci.appveyor.com/api/projects/Kiritsu/FoxBot/artifacts/FoxUbuntu.zip
unzip -o ./FoxUbuntu.zip
rm ./FoxUbuntu.zip
chmod +x ./Fox
nohup ./Fox &