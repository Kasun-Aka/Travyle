import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import '../../models/support_ticket.dart';
import '../../services/support_api_service.dart';
import '../../theme/app_theme.dart';
import 'ticket_detail_screen.dart';

class SupportCenterScreen extends StatefulWidget {
  const SupportCenterScreen({super.key});

  @override
  State<SupportCenterScreen> createState() => _SupportCenterScreenState();
}

class _SupportCenterScreenState extends State<SupportCenterScreen> with SingleTickerProviderStateMixin {
  late TabController _tabController;
  final SupportApiService _apiService = SupportApiService();

  // Form state
  final _formKey = GlobalKey<FormState>();
  final _titleController = TextEditingController();
  final _descController = TextEditingController();
  String _selectedCategory = 'TourDelay';
  String _selectedPriority = 'High';
  Uint8List? _attachedImageBytes;
  bool _isSubmitting = false;

  // Tickets state
  List<SupportTicketModel> _tickets = [];
  bool _isLoadingTickets = true;

  final ImagePicker _picker = ImagePicker();

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 2, vsync: this);
    _loadTickets();
  }

  @override
  void dispose() {
    _tabController.dispose();
    _titleController.dispose();
    _descController.dispose();
    super.dispose();
  }

  Future<void> _loadTickets() async {
    setState(() => _isLoadingTickets = true);
    final results = await _apiService.getTickets();
    if (!mounted) return;
    setState(() {
      _tickets = results;
      _isLoadingTickets = false;
    });
  }

  Future<void> _pickImage(ImageSource source) async {
    try {
      final picked = await _picker.pickImage(source: source, maxWidth: 1024, maxHeight: 1024, imageQuality: 85);
      if (picked != null) {
        final bytes = await picked.readAsBytes();
        setState(() {
          _attachedImageBytes = bytes;
          _attachedImageName = picked.name;
        });
      }
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Error accessing camera/gallery: $e')),
      );
    }
  }

  void _showImageSourceDialog() {
    showModalBottomSheet(
      context: context,
      shape: const RoundedRectangleBorder(borderRadius: BorderRadius.vertical(top: Radius.circular(20))),
      builder: (ctx) => SafeArea(
        child: Wrap(
          children: [
            ListTile(
              leading: const Icon(Icons.photo_camera, color: AppTheme.primaryDark),
              title: const Text('Capture with Camera'),
              onTap: () {
                Navigator.pop(ctx);
                _pickImage(ImageSource.camera);
              },
            ),
            ListTile(
              leading: const Icon(Icons.photo_library, color: AppTheme.primaryDark),
              title: const Text('Choose from Gallery'),
              onTap: () {
                Navigator.pop(ctx);
                _pickImage(ImageSource.gallery);
              },
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _submitTicket() async {
    if (!_formKey.currentState!.validate()) return;

    setState(() => _isSubmitting = true);

    // If an image was selected, simulate cloud photo attachment URL or pass image path
    final simulatedAttachmentUrl = _attachedImageBytes != null
        ? 'https://images.unsplash.com/photo-1544620347-c4fd4a3d5957?w=600'
        : null;

    final created = await _apiService.createTicket(
      title: _titleController.text.trim(),
      description: _descController.text.trim(),
      category: _selectedCategory,
      priority: _selectedPriority,
      attachmentUrl: simulatedAttachmentUrl,
    );

    if (!mounted) return;
    setState(() => _isSubmitting = false);

    if (created != null) {
      _titleController.clear();
      _descController.clear();
      setState(() {
        _attachedImageBytes = null;
        _attachedImageName = null;
      });

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Support ticket created! AI Agent assigned status: ${created.status}'),
          backgroundColor: Colors.green,
        ),
      );

      _tabController.animateTo(1);
      _loadTickets();
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Failed to submit ticket. Check backend connection.'),
          backgroundColor: Colors.redAccent,
        ),
      );
    }
  }

  Color _getStatusColor(String status) {
    switch (status) {
      case 'Pending_Admin_Voucher_Approval':
        return Colors.amber.shade800;
      case 'Pending_AI_Triage':
        return Colors.indigo;
      case 'Resolved':
        return Colors.green.shade700;
      case 'In_Review':
        return Colors.blue.shade700;
      default:
        return Colors.grey.shade700;
    }
  }

  String _formatStatus(String status) {
    switch (status) {
      case 'Pending_Admin_Voucher_Approval':
        return '⏳ Pending Voucher Approval';
      case 'Pending_AI_Triage':
        return '🤖 AI Triaging';
      case 'In_Review':
        return '🔍 In Review';
      case 'Resolved':
        return '✓ Resolved';
      default:
        return status;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Traveler Support Center', style: TextStyle(fontWeight: FontWeight.bold, color: Colors.white)),
        backgroundColor: AppTheme.primaryDark,
        elevation: 0,
        bottom: TabBar(
          controller: _tabController,
          indicatorColor: AppTheme.coral,
          indicatorWeight: 3,
          labelColor: Colors.white,
          unselectedLabelColor: Colors.white70,
          tabs: const [
            Tab(icon: Icon(Icons.add_comment_outlined), text: 'New Claim / Ticket'),
            Tab(icon: Icon(Icons.list_alt_outlined), text: 'My Tickets'),
          ],
        ),
      ),
      body: TabBarView(
        controller: _tabController,
        children: [
          _buildNewTicketForm(),
          _buildTicketsList(),
        ],
      ),
    );
  }

  Widget _buildNewTicketForm() {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(20),
      child: Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Report an Issue / Tour Dispute',
              style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold, color: AppTheme.primaryDark),
            ),
            const SizedBox(height: 6),
            const Text(
              'Our AI Support Agent will automatically triage your claim, assess delay severity, and draft goodwill compensation.',
              style: TextStyle(fontSize: 14, color: AppTheme.textLight),
            ),
            const SizedBox(height: 20),

            // Subject
            const Text('Issue Subject', style: TextStyle(fontWeight: FontWeight.w600, color: AppTheme.textDark)),
            const SizedBox(height: 8),
            TextFormField(
              controller: _titleController,
              decoration: const InputDecoration(
                hintText: 'e.g. 3-Hour Bus Delay on Tour Excursion',
              ),
              validator: (v) => (v == null || v.trim().isEmpty) ? 'Please enter a title' : null,
            ),
            const SizedBox(height: 16),

            // Category & Priority
            Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text('Category', style: TextStyle(fontWeight: FontWeight.w600, color: AppTheme.textDark)),
                      const SizedBox(height: 8),
                      DropdownButtonFormField<String>(
                        initialValue: _selectedCategory,
                        decoration: const InputDecoration(contentPadding: EdgeInsets.symmetric(horizontal: 14, vertical: 12)),
                        items: const [
                          DropdownMenuItem(value: 'TourDelay', child: Text('Tour Delay')),
                          DropdownMenuItem(value: 'TourQuality', child: Text('Tour Quality')),
                          DropdownMenuItem(value: 'Safety', child: Text('Safety Issue')),
                          DropdownMenuItem(value: 'Billing', child: Text('Billing / Payment')),
                          DropdownMenuItem(value: 'GuideConduct', child: Text('Guide Conduct')),
                          DropdownMenuItem(value: 'General', child: Text('General Inquiry')),
                        ],
                        onChanged: (val) {
                          if (val != null) setState(() => _selectedCategory = val);
                        },
                      ),
                    ],
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text('Priority', style: TextStyle(fontWeight: FontWeight.w600, color: AppTheme.textDark)),
                      const SizedBox(height: 8),
                      DropdownButtonFormField<String>(
                        initialValue: _selectedPriority,
                        decoration: const InputDecoration(contentPadding: EdgeInsets.symmetric(horizontal: 14, vertical: 12)),
                        items: const [
                          DropdownMenuItem(value: 'Critical', child: Text('Critical')),
                          DropdownMenuItem(value: 'High', child: Text('High')),
                          DropdownMenuItem(value: 'Medium', child: Text('Medium')),
                          DropdownMenuItem(value: 'Low', child: Text('Low')),
                        ],
                        onChanged: (val) {
                          if (val != null) setState(() => _selectedPriority = val);
                        },
                      ),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),

            // Description
            const Text('Detailed Description', style: TextStyle(fontWeight: FontWeight.w600, color: AppTheme.textDark)),
            const SizedBox(height: 8),
            TextFormField(
              controller: _descController,
              maxLines: 4,
              decoration: const InputDecoration(
                hintText: 'Please describe the incident, delays, missed stops, or concerns in detail...',
              ),
              validator: (v) => (v == null || v.trim().isEmpty) ? 'Please provide detailed description' : null,
            ),
            const SizedBox(height: 18),

            // Camera / Photo Attachment
            const Text('Photo Evidence (Camera / Gallery)', style: TextStyle(fontWeight: FontWeight.w600, color: AppTheme.textDark)),
            const SizedBox(height: 8),
            if (_attachedImageBytes != null) ...[
              Stack(
                alignment: Alignment.topRight,
                children: [
                  ClipRRect(
                    borderRadius: BorderRadius.circular(12),
                    child: Image.memory(_attachedImageBytes!, height: 160, width: double.infinity, fit: BoxFit.cover),
                  ),
                  IconButton(
                    icon: const Icon(Icons.cancel, color: Colors.red),
                    onPressed: () => setState(() {
                      _attachedImageBytes = null;
                      _attachedImageName = null;
                    }),
                  ),
                ],
              ),
              const SizedBox(height: 10),
            ] else ...[
              OutlinedButton.icon(
                onPressed: _showImageSourceDialog,
                icon: const Icon(Icons.camera_alt_outlined, color: AppTheme.primaryDark),
                label: const Text('Attach Incident Photo via Camera', style: TextStyle(color: AppTheme.primaryDark)),
                style: OutlinedButton.styleFrom(
                  padding: const EdgeInsets.symmetric(vertical: 14, horizontal: 16),
                  side: const BorderSide(color: Color(0xFFCBD5E1)),
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                ),
              ),
            ],
            const SizedBox(height: 28),

            // Submit Button
            SizedBox(
              width: double.infinity,
              height: 52,
              child: ElevatedButton(
                onPressed: _isSubmitting ? null : _submitTicket,
                style: ElevatedButton.styleFrom(
                  backgroundColor: AppTheme.primaryDark,
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(30)),
                ),
                child: _isSubmitting
                  ? const CircularProgressIndicator(color: Colors.white)
                  : const Text(
                      'Submit Claim to AI Support Agent',
                      style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: Colors.white),
                    ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildTicketsList() {
    if (_isLoadingTickets) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_tickets.isEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(Icons.support_agent_outlined, size: 64, color: AppTheme.textLight),
            const SizedBox(height: 12),
            const Text('No Support Tickets Filed Yet', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
            const SizedBox(height: 8),
            TextButton(
              onPressed: () => _tabController.animateTo(0),
              child: const Text('Submit your first claim &rarr;'),
            )
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: _loadTickets,
      child: ListView.separated(
        padding: const EdgeInsets.all(16),
        itemCount: _tickets.length,
        separatorBuilder: (context, index) => const SizedBox(height: 12),
        itemBuilder: (context, index) {
          final t = _tickets[index];
          return Card(
            elevation: 2,
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
            child: InkWell(
              borderRadius: BorderRadius.circular(14),
              onTap: () async {
                await Navigator.push(
                  context,
                  MaterialPageRoute(builder: (_) => TicketDetailScreen(ticketId: t.id)),
                );
                _loadTickets();
              },
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Container(
                          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                          decoration: BoxDecoration(
                            color: _getStatusColor(t.status).withAlpha(30),
                            borderRadius: BorderRadius.circular(20),
                          ),
                          child: Text(
                            _formatStatus(t.status),
                            style: TextStyle(
                              color: _getStatusColor(t.status),
                              fontSize: 12,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                        ),
                        Text(
                          '${t.createdAt.month}/${t.createdAt.day}',
                          style: const TextStyle(fontSize: 12, color: AppTheme.textLight),
                        ),
                      ],
                    ),
                    const SizedBox(height: 10),
                    Text(
                      t.title,
                      style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: AppTheme.textDark),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      t.description,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(fontSize: 13, color: Color(0xFF64748B)),
                    ),
                    const SizedBox(height: 12),
                    Row(
                      children: [
                        Container(
                          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                          decoration: BoxDecoration(
                            color: Colors.grey.shade200,
                            borderRadius: BorderRadius.circular(4),
                          ),
                          child: Text(t.category, style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w600)),
                        ),
                        const Spacer(),
                        const Text('View Timeline &rarr;', style: TextStyle(color: AppTheme.coral, fontWeight: FontWeight.bold, fontSize: 13)),
                      ],
                    ),
                  ],
                ),
              ),
            ),
          );
        },
      ),
    );
  }
}
