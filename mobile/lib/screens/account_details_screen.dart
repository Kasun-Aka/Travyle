import 'package:flutter/material.dart';
import 'package:firebase_auth/firebase_auth.dart';
import 'package:dio/dio.dart';
import '../theme/app_theme.dart';
import '../widgets/gradient_button.dart';

class AccountDetailsScreen extends StatefulWidget {
  final String role;

  const AccountDetailsScreen({super.key, required this.role});

  @override
  State<AccountDetailsScreen> createState() => _AccountDetailsScreenState();
}

class _AccountDetailsScreenState extends State<AccountDetailsScreen> {
  final User? _currentUser = FirebaseAuth.instance.currentUser;
  late final TextEditingController _fullNameController;
  late final TextEditingController _emailController;
  bool _isLoading = false;
  String _errorMessage = '';
  String _successMessage = '';

  @override
  void initState() {
    super.initState();
    _fullNameController = TextEditingController(text: _currentUser?.displayName ?? '');
    _emailController = TextEditingController(text: _currentUser?.email ?? '');
  }

  @override
  void dispose() {
    _fullNameController.dispose();
    _emailController.dispose();
    super.dispose();
  }

  Future<void> _handleSave() async {
    final newName = _fullNameController.text.trim();
    if (newName.isEmpty) {
      setState(() => _errorMessage = 'Full Name cannot be empty.');
      return;
    }

    setState(() {
      _isLoading = true;
      _errorMessage = '';
      _successMessage = '';
    });

    try {
      // 1. Update Firebase
      await _currentUser?.updateDisplayName(newName);

      // 2. Update PostgreSQL Backend
      final dio = Dio();
      await dio.put('http://10.0.2.2:5085/api/auth/user', data: {
        'email': _currentUser?.email,
        'fullName': newName,
      });

      setState(() {
        _successMessage = 'Profile updated successfully!';
      });
      
      // Update previous screen
      if (mounted) {
        Navigator.pop(context, true); // Return true to indicate change
      }
    } catch (e) {
      debugPrint('Error updating profile: $e');
      setState(() {
        _errorMessage = 'Failed to update account details.';
      });
    } finally {
      if (mounted) {
        setState(() {
          _isLoading = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        backgroundColor: Colors.transparent,
        elevation: 0,
        title: const Text('Account Details', style: TextStyle(color: AppTheme.textDark, fontSize: 18, fontWeight: FontWeight.bold)),
        centerTitle: true,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back, color: AppTheme.textDark),
          onPressed: () => Navigator.pop(context),
        ),
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: 24.0, vertical: 20.0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              if (_errorMessage.isNotEmpty) ...[
                Container(
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: Colors.red.shade50,
                    borderRadius: BorderRadius.circular(8),
                    border: Border.all(color: Colors.red.shade200),
                  ),
                  child: Row(
                    children: [
                      Icon(Icons.error_outline, color: Colors.red.shade700, size: 20),
                      const SizedBox(width: 8),
                      Expanded(
                        child: Text(_errorMessage, style: TextStyle(color: Colors.red.shade700, fontSize: 13)),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 24),
              ],
              
              if (_successMessage.isNotEmpty) ...[
                Container(
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: Colors.green.shade50,
                    borderRadius: BorderRadius.circular(8),
                    border: Border.all(color: Colors.green.shade200),
                  ),
                  child: Row(
                    children: [
                      Icon(Icons.check_circle_outline, color: Colors.green.shade700, size: 20),
                      const SizedBox(width: 8),
                      Expanded(
                        child: Text(_successMessage, style: TextStyle(color: Colors.green.shade700, fontSize: 13)),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 24),
              ],

              const Text('FULL NAME', style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: AppTheme.textDark)),
              const SizedBox(height: 8),
              TextFormField(
                controller: _fullNameController,
                decoration: const InputDecoration(hintText: 'Enter your full name'),
              ),
              const SizedBox(height: 24),

              const Text('EMAIL ADDRESS (READ-ONLY)', style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: AppTheme.textGrey)),
              const SizedBox(height: 8),
              TextFormField(
                controller: _emailController,
                readOnly: true,
                style: const TextStyle(color: AppTheme.textGrey),
                decoration: InputDecoration(
                  fillColor: Colors.grey.shade100,
                ),
              ),
              const SizedBox(height: 24),
              
              const Text('ACCOUNT ROLE', style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: AppTheme.textGrey)),
              const SizedBox(height: 8),
              TextFormField(
                initialValue: widget.role.toUpperCase(),
                readOnly: true,
                style: const TextStyle(color: AppTheme.textGrey, fontWeight: FontWeight.bold),
                decoration: InputDecoration(
                  fillColor: Colors.grey.shade100,
                ),
              ),
              const SizedBox(height: 40),
              
              _isLoading
                  ? const Center(child: CircularProgressIndicator(color: AppTheme.primaryDark))
                  : GradientButton(
                      text: 'SAVE CHANGES',
                      onPressed: _handleSave,
                    ),
            ],
          ),
        ),
      ),
    );
  }
}
