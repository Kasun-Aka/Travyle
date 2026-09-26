with open("mobile/lib/screens/home_screen.dart", "r", encoding="utf-8") as f:
    text = f.read()

old_text = """                            child: Text(
                              '',
                              style: const TextStyle(
                                color: Colors.white,
                                fontSize: 10,"""

new_text = """                            child: Text(
                              '$_unreadNotifications',
                              style: const TextStyle(
                                color: Colors.white,
                                fontSize: 10,"""

text = text.replace(old_text, new_text)

with open("mobile/lib/screens/home_screen.dart", "w", encoding="utf-8") as f:
    f.write(text)

print("Count fixed!")
