with open("mobile/lib/screens/preference_screen.dart", "r", encoding="utf-8") as f:
    text = f.read()

# 1. change default 'selected': true to 'selected': false
text = text.replace("'selected': true,", "'selected': false,")

# 2. Insert initState
old_state_start = """class _PreferenceScreenState extends State<PreferenceScreen> {
  bool _isSaving = false;"""

new_state_start = """class _PreferenceScreenState extends State<PreferenceScreen> {
  bool _isSaving = false;
  bool _isLoadingPrefs = true;

  @override
  void initState() {
    super.initState();
    _loadPreferences();
  }

  Future<void> _loadPreferences() async {
    final user = FirebaseAuth.instance.currentUser;
    if (user == null || user.email == null) {
      if (mounted) setState(() => _isLoadingPrefs = false);
      return;
    }
    try {
      final response = await Dio().get(
        'http://10.0.2.2:5085/api/auth/user/preferences',
        queryParameters: {'email': user.email},
      );
      if (response.statusCode == 200 && mounted) {
        final List<dynamic> prefs = response.data;
        setState(() {
          for (var item in _preferences) {
            item['selected'] = prefs.contains(item['title']);
          }
          _isLoadingPrefs = false;
        });
      }
    } catch (e) {
      debugPrint('Failed to load prefs: $e');
      if (mounted) setState(() => _isLoadingPrefs = false);
    }
  }"""

text = text.replace(old_state_start, new_state_start)

# 3. Add loading indicator to the build method
old_build = """  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppTheme.backgroundLight,
      appBar: AppBar("""

new_build = """  Widget build(BuildContext context) {
    if (_isLoadingPrefs) {
      return const Scaffold(
        backgroundColor: AppTheme.backgroundLight,
        body: Center(child: CircularProgressIndicator()),
      );
    }
    
    return Scaffold(
      backgroundColor: AppTheme.backgroundLight,
      appBar: AppBar("""

text = text.replace(old_build, new_build)

with open("mobile/lib/screens/preference_screen.dart", "w", encoding="utf-8") as f:
    f.write(text)

print("Updated preference_screen.dart")
