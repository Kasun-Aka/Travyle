with open("mobile/lib/screens/home_screen.dart", "r", encoding="utf-8") as f:
    text = f.read()

import re

# 1. Update BottomNavigationBar onTap
old_nav = """        bottomNavigationBar: BottomNavigationBar(
          currentIndex: _currentIndex,
          onTap: (index) {
            setState(() {
              _currentIndex = index;
            });
          },"""

new_nav = """        bottomNavigationBar: BottomNavigationBar(
          currentIndex: _currentIndex,
          onTap: (index) {
            setState(() {
              _currentIndex = index;
            });
            if (index == 0) {
              _fetchNotifications();
              _fetchDestinations();
            }
          },"""

text = text.replace(old_nav, new_nav)

# 2. Add RefreshIndicator
old_build = """  Widget _buildHomeContent(BuildContext context) {
    return SingleChildScrollView("""

new_build = """  Future<void> _onRefresh() async {
    await _fetchNotifications();
    await _fetchDestinations();
  }

  Widget _buildHomeContent(BuildContext context) {
    return RefreshIndicator(
      onRefresh: _onRefresh,
      color: AppTheme.accentCopper,
      child: SingleChildScrollView("""

text = text.replace(old_build, new_build)

# 3. Add physics: AlwaysScrollableScrollPhysics() so the RefreshIndicator always works even if list is small
old_scroll = """      child: SingleChildScrollView(
        padding: const EdgeInsets.symmetric(horizontal: 24.0, vertical: 20.0),"""
new_scroll = """      child: SingleChildScrollView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.symmetric(horizontal: 24.0, vertical: 20.0),"""
text = text.replace(old_scroll, new_scroll)


with open("mobile/lib/screens/home_screen.dart", "w", encoding="utf-8") as f:
    f.write(text)

print("Updated home_screen.dart")
