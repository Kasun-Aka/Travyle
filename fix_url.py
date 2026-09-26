with open("mobile/lib/screens/home_screen.dart", "r", encoding="utf-8") as f:
    text = f.read()

text = text.replace(
    "'http://10.0.2.2:5085/api/notifications/traveler/',",
    "'http://10.0.2.2:5085/api/notifications/traveler/$email',"
)

text = text.replace(
    "'Failed to load notifications: '",
    "'Failed to load notifications: $e'"
)

with open("mobile/lib/screens/home_screen.dart", "w", encoding="utf-8") as f:
    f.write(text)

print("URL Fixed!")
