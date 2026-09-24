import 'package:flutter/material.dart';
import 'package:firebase_auth/firebase_auth.dart';
import 'package:dio/dio.dart';
import 'package:image_picker/image_picker.dart';
import 'package:image_cropper/image_cropper.dart';
import '../theme/app_theme.dart';
import 'login_screen.dart';
import 'account_details_screen.dart';

class ProfileScreen extends StatefulWidget {
  const ProfileScreen({super.key});

  @override
  State<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends State<ProfileScreen> {
  final User? _currentUser = FirebaseAuth.instance.currentUser;
  bool _isLoading = true;
  String _role = 'Traveler'; // Default fallback
  String _errorMessage = '';

  @override
  void initState() {
    super.initState();
    _fetchUserDetails();
  }

  Future<void> _fetchUserDetails() async {
    if (_currentUser?.email == null) {
      if (mounted) setState(() => _isLoading = false);
      return;
    }

    try {
      final dio = Dio(BaseOptions(
        connectTimeout: const Duration(seconds: 3),
        receiveTimeout: const Duration(seconds: 3),
      ));
      final response = await dio.get('http://10.0.2.2:5085/api/auth/user?email=${_currentUser!.email}');
      
      if (response.statusCode == 200 && response.data != null) {
        if (mounted) {
          setState(() {
            _role = response.data['role'] ?? 'Traveler';
          });
        }
      }
    } catch (e) {
      debugPrint('Error fetching user role: $e');
      if (mounted) {
        setState(() {
          _errorMessage = 'Could not load complete profile data.';
        });
      }
    } finally {
      if (mounted) {
        setState(() {
          _isLoading = false;
        });
      }
    }
  }

  Future<void> _handleLogout() async {
    await FirebaseAuth.instance.signOut();
    if (mounted) {
      Navigator.of(context, rootNavigator: true).pushReplacement(
        MaterialPageRoute(builder: (context) => const LoginScreen()),
      );
    }
  }

  Future<void> _showAvatarPicker() async {
    try {
      final ImagePicker picker = ImagePicker();
      final XFile? image = await picker.pickImage(source: ImageSource.gallery);
      
      if (image == null) return;

      final ImageCropper cropper = ImageCropper();
      final CroppedFile? croppedFile = await cropper.cropImage(
        sourcePath: image.path,
        aspectRatio: const CropAspectRatio(ratioX: 1, ratioY: 1),
        uiSettings: [
          AndroidUiSettings(
            toolbarTitle: 'Crop Profile Picture',
            toolbarColor: AppTheme.primaryDark,
            toolbarWidgetColor: Colors.white,
            initAspectRatio: CropAspectRatioPreset.square,
            lockAspectRatio: true,
          ),
          IOSUiSettings(
            title: 'Crop Profile Picture',
            aspectRatioLockEnabled: true,
            resetAspectRatioEnabled: false,
          ),
        ],
      );

      if (croppedFile == null) return;

      setState(() => _isLoading = true);

      final dio = Dio();
      final formData = FormData.fromMap({
        'email': _currentUser?.email,
        'file': await MultipartFile.fromFile(croppedFile.path),
      });

      final response = await dio.post('http://10.0.2.2:5085/api/auth/avatar', data: formData);

      if (response.statusCode == 200 && response.data != null) {
        final newUrl = response.data['url'];
        await _currentUser?.updatePhotoURL(newUrl);
      }
    } catch (e) {
      debugPrint('Error updating avatar: $e');
      if (mounted) {
        setState(() => _errorMessage = 'Failed to upload profile picture.');
      }
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) {
      return const Center(child: CircularProgressIndicator(color: AppTheme.primaryDark));
    }

    final avatarUrl = _currentUser?.photoURL ?? 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=200&h=200&fit=crop';

    return SingleChildScrollView(
      padding: const EdgeInsets.symmetric(horizontal: 24.0, vertical: 20.0),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          const SizedBox(height: 20),
          Text('My Profile', style: Theme.of(context).textTheme.displayLarge),
          const SizedBox(height: 32),
          
          // Avatar
          GestureDetector(
            onTap: _showAvatarPicker,
            child: Stack(
              children: [
                CircleAvatar(
                  radius: 60,
                  backgroundImage: NetworkImage(avatarUrl),
                ),
                Positioned(
                  bottom: 0,
                  right: 0,
                  child: Container(
                    padding: const EdgeInsets.all(8),
                    decoration: BoxDecoration(
                      color: AppTheme.accentCopper,
                      shape: BoxShape.circle,
                      border: Border.all(color: AppTheme.backgroundLight, width: 3),
                    ),
                    child: const Icon(Icons.edit, color: Colors.white, size: 20),
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 24),
          
          Text(
            _currentUser?.displayName ?? 'Traveler',
            style: const TextStyle(fontSize: 24, fontWeight: FontWeight.bold, color: AppTheme.primaryDark),
          ),
          const SizedBox(height: 4),
          Text(
            _currentUser?.email ?? '',
            style: const TextStyle(fontSize: 14, color: AppTheme.textGrey),
          ),
          const SizedBox(height: 12),
          
          // Role Badge
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 6),
            decoration: BoxDecoration(
              color: AppTheme.primaryDark.withValues(alpha: 0.1),
              borderRadius: BorderRadius.circular(20),
            ),
            child: Text(
              _role.toUpperCase(),
              style: const TextStyle(
                color: AppTheme.primaryDark,
                fontWeight: FontWeight.bold,
                fontSize: 12,
                letterSpacing: 1.0,
              ),
            ),
          ),
          const SizedBox(height: 40),

          if (_errorMessage.isNotEmpty)
            Padding(
              padding: const EdgeInsets.only(bottom: 16.0),
              child: Text(_errorMessage, style: TextStyle(color: Colors.red.shade700, fontSize: 12)),
            ),

          // Menu Options
          _buildMenuOption(
            icon: Icons.person_outline, 
            title: 'Account Details',
            onTap: () async {
              final result = await Navigator.push(
                context,
                MaterialPageRoute(builder: (context) => AccountDetailsScreen(role: _role)),
              );
              if (result == true && mounted) {
                // Refresh to get updated displayName
                setState(() {});
              }
            }
          ),
          _buildMenuOption(icon: Icons.notifications_outlined, title: 'Notifications', onTap: () {}),
          _buildMenuOption(icon: Icons.security, title: 'Privacy & Security', onTap: () {}),
          _buildMenuOption(icon: Icons.help_outline, title: 'Help & Support', onTap: () {}),
          
          const SizedBox(height: 32),
          
          OutlinedButton.icon(
            onPressed: _handleLogout,
            icon: const Icon(Icons.logout, color: Colors.redAccent),
            label: const Text('Log Out', style: TextStyle(color: Colors.redAccent, fontWeight: FontWeight.bold)),
            style: OutlinedButton.styleFrom(
              padding: const EdgeInsets.symmetric(vertical: 16, horizontal: 24),
              side: const BorderSide(color: Colors.redAccent),
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(30),
              ),
              minimumSize: const Size(double.infinity, 50),
            ),
          ),
          const SizedBox(height: 40),
        ],
      ),
    );
  }

  Widget _buildMenuOption({required IconData icon, required String title, required VoidCallback onTap}) {
    return ListTile(
      contentPadding: EdgeInsets.zero,
      leading: Container(
        padding: const EdgeInsets.all(10),
        decoration: BoxDecoration(
          color: AppTheme.backgroundLight,
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: AppTheme.borderLight),
        ),
        child: Icon(icon, color: AppTheme.primaryDark, size: 22),
      ),
      title: Text(
        title,
        style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600, color: AppTheme.textDark),
      ),
      trailing: const Icon(Icons.chevron_right, color: AppTheme.textGrey),
      onTap: onTap,
    );
  }
}
