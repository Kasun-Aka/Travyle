import re

with open("mobile/lib/screens/preference_screen.dart", "r", encoding="utf-8") as f:
    text = f.read()

old_build = "  Widget build(BuildContext context) {"
new_build = """  @override
  Widget build(BuildContext context) {
    if (_isLoadingPrefs) {
      return const Scaffold(
        backgroundColor: Colors.white,
        body: Center(child: CircularProgressIndicator()),
      );
    }
"""

text = text.replace("  @override\n  Widget build(BuildContext context) {", new_build)

with open("mobile/lib/screens/preference_screen.dart", "w", encoding="utf-8") as f:
    f.write(text)

print("Fixed build method")
