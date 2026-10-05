import 'dart:convert';
import 'package:flutter/material.dart';
import '../../models/support_ticket.dart';
import '../../services/support_api_service.dart';
import '../../theme/app_theme.dart';

class TicketDetailScreen extends StatefulWidget {
  final String ticketId;

  const TicketDetailScreen({super.key, required this.ticketId});

  @override
  State<TicketDetailScreen> createState() => _TicketDetailScreenState();
}

class _TicketDetailScreenState extends State<TicketDetailScreen> {
  final SupportApiService _apiService = SupportApiService();
  final TextEditingController _followupController = TextEditingController();
  SupportTicketModel? _ticket;
  bool _isLoading = true;
  bool _isSubmittingFollowup = false;

  @override
  void initState() {
    super.initState();
    _loadTicket();
  }

  @override
  void dispose() {
    _followupController.dispose();
    super.dispose();
  }

  Future<void> _submitFollowupNote() async {
    final note = _followupController.text.trim();
    if (note.isEmpty) return;

    setState(() => _isSubmittingFollowup = true);
    final updatedTicket = await _apiService.addFollowupNote(widget.ticketId, note);
    if (!mounted) return;
    setState(() => _isSubmittingFollowup = false);

    if (updatedTicket != null) {
      _followupController.clear();
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Follow-up note added successfully.')),
      );
      _loadTicket();
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Failed to submit follow-up note.')),
      );
    }
  }

  Future<void> _loadTicket() async {
    setState(() => _isLoading = true);
    final data = await _apiService.getTicketById(widget.ticketId);
    if (!mounted) return;
    setState(() {
      _ticket = data;
      _isLoading = false;
    });
  }

  bool get _canCancel =>
      _ticket != null &&
      (_ticket!.status == 'Pending_AI_Triage' || _ticket!.status == 'In_Review');

  Future<void> _showCancelConfirmationDialog() async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Cancel Ticket?'),
        content: const Text(
          'Are you sure you want to cancel this ticket? This action cannot be undone.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(false),
            child: const Text('Keep Ticket'),
          ),
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(true),
            style: TextButton.styleFrom(foregroundColor: Colors.red),
            child: const Text('Yes, Cancel Ticket'),
          ),
        ],
      ),
    );

    if (confirmed == true) {
      setState(() => _isLoading = true);
      final updatedTicket = await _apiService.cancelTicket(widget.ticketId);
      if (!mounted) return;
      if (updatedTicket != null) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Ticket cancelled successfully.')),
        );
        _loadTicket();
      } else {
        setState(() => _isLoading = false);
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Failed to cancel ticket. Please try again.')),
        );
      }
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
      case 'Closed':
        return Colors.grey.shade700;
      default:
        return Colors.grey.shade700;
    }
  }

  String _formatStatus(String status) {
    switch (status) {
      case 'Pending_Admin_Voucher_Approval':
        return '⏳ Awaiting Admin Voucher Approval';
      case 'Pending_AI_Triage':
        return '🤖 AI Triaging Claim';
      case 'In_Review':
        return '🔍 In Review';
      case 'Resolved':
        return '✓ Claim Resolved';
      case 'Closed':
        return '🚫 Ticket Cancelled / Closed';
      default:
        return status;
    }
  }

  Widget _buildAttachmentImage(String url) {
    if (url.startsWith('data:image')) {
      try {
        final base64Str = url.split(',').last;
        final bytes = base64Decode(base64Str);
        return Image.memory(
          bytes,
          height: 180,
          width: double.infinity,
          fit: BoxFit.cover,
          errorBuilder: (context, error, stackTrace) => const Icon(Icons.broken_image, size: 60),
        );
      } catch (_) {}
    }
    return Image.network(
      url,
      height: 180,
      width: double.infinity,
      fit: BoxFit.cover,
      errorBuilder: (context, error, stackTrace) => const Icon(Icons.broken_image, size: 60),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Claim Details & Trace', style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold)),
        backgroundColor: AppTheme.primaryDark,
        iconTheme: const IconThemeData(color: Colors.white),
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : _ticket == null
              ? const Center(child: Text('Ticket not found'))
              : RefreshIndicator(
                  onRefresh: _loadTicket,
                  child: SingleChildScrollView(
                    padding: const EdgeInsets.all(20),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        // Status Header Card
                        Container(
                          width: double.infinity,
                          padding: const EdgeInsets.all(16),
                          decoration: BoxDecoration(
                            color: _getStatusColor(_ticket!.status).withAlpha(20),
                            borderRadius: BorderRadius.circular(14),
                            border: Border.all(color: _getStatusColor(_ticket!.status).withAlpha(75)),
                          ),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Row(
                                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                children: [
                                  Text(
                                    _formatStatus(_ticket!.status),
                                    style: TextStyle(
                                      color: _getStatusColor(_ticket!.status),
                                      fontWeight: FontWeight.bold,
                                      fontSize: 14,
                                    ),
                                  ),
                                  Text(
                                    'Priority: ${_ticket!.priority}',
                                    style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 12),
                                  ),
                                ],
                              ),
                              const SizedBox(height: 8),
                              Text(
                                _ticket!.title,
                                style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: AppTheme.primaryDark),
                              ),
                            ],
                          ),
                        ),
                        if (_canCancel) ...[
                          const SizedBox(height: 12),
                          SizedBox(
                            width: double.infinity,
                            child: OutlinedButton.icon(
                              onPressed: _showCancelConfirmationDialog,
                              icon: const Icon(Icons.cancel_outlined, color: Colors.red),
                              label: const Text(
                                'Cancel Ticket',
                                style: TextStyle(color: Colors.red, fontWeight: FontWeight.bold),
                              ),
                              style: OutlinedButton.styleFrom(
                                side: const BorderSide(color: Colors.red),
                                padding: const EdgeInsets.symmetric(vertical: 12),
                                shape: RoundedRectangleBorder(
                                  borderRadius: BorderRadius.circular(10),
                                ),
                              ),
                            ),
                          ),
                        ],
                        const SizedBox(height: 20),

                        // Description & Photo
                        const Text('Your Statement', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: AppTheme.textDark)),
                        const SizedBox(height: 8),
                        Container(
                          width: double.infinity,
                          padding: const EdgeInsets.all(16),
                          decoration: BoxDecoration(
                            color: Colors.white,
                            borderRadius: BorderRadius.circular(12),
                            border: Border.all(color: const Color(0xFFE2E8F0)),
                          ),
                          child: Text(_ticket!.description, style: const TextStyle(fontSize: 14, height: 1.5)),
                        ),
                        if (_ticket!.attachmentUrl != null) ...[
                          const SizedBox(height: 14),
                          const Text('Attached Photo Evidence', style: TextStyle(fontSize: 14, fontWeight: FontWeight.w600)),
                          const SizedBox(height: 8),
                          ClipRRect(
                            borderRadius: BorderRadius.circular(12),
                            child: _buildAttachmentImage(_ticket!.attachmentUrl!),
                          ),
                        ],
                        const SizedBox(height: 24),

                        // Follow-up Notes Section
                        Builder(
                          builder: (context) {
                            final followups = _ticket!.auditLogs
                                .where((log) => log.action == 'TRAVELER_FOLLOWUP')
                                .toList()
                              ..sort((a, b) => a.timestamp.compareTo(b.timestamp));
                            return Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                const Text(
                                  'Follow-up Notes',
                                  style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: AppTheme.textDark),
                                ),
                                const SizedBox(height: 8),
                                if (followups.isNotEmpty)
                                  ListView.separated(
                                    shrinkWrap: true,
                                    physics: const NeverScrollableScrollPhysics(),
                                    itemCount: followups.length,
                                    separatorBuilder: (context, index) => const SizedBox(height: 8),
                                    itemBuilder: (context, index) {
                                      final item = followups[index];
                                      return Container(
                                        width: double.infinity,
                                        padding: const EdgeInsets.all(14),
                                        decoration: BoxDecoration(
                                          color: const Color(0xFFF8FAFC),
                                          borderRadius: BorderRadius.circular(12),
                                          border: Border.all(color: const Color(0xFFCBD5E1)),
                                        ),
                                        child: Column(
                                          crossAxisAlignment: CrossAxisAlignment.start,
                                          children: [
                                            Row(
                                              mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                              children: [
                                                const Text(
                                                  'Traveler Follow-up',
                                                  style: TextStyle(fontWeight: FontWeight.bold, fontSize: 13, color: AppTheme.primaryDark),
                                                ),
                                                Text(
                                                  '${item.timestamp.day}/${item.timestamp.month} ${item.timestamp.hour}:${item.timestamp.minute.toString().padLeft(2, '0')}',
                                                  style: const TextStyle(fontSize: 11, color: AppTheme.textLight),
                                                ),
                                              ],
                                            ),
                                            const SizedBox(height: 6),
                                            Text(item.details, style: const TextStyle(fontSize: 14, height: 1.4)),
                                          ],
                                        ),
                                      );
                                    },
                                  ),
                                const SizedBox(height: 10),
                                Container(
                                  padding: const EdgeInsets.all(12),
                                  decoration: BoxDecoration(
                                    color: Colors.white,
                                    borderRadius: BorderRadius.circular(12),
                                    border: Border.all(color: const Color(0xFFE2E8F0)),
                                  ),
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      TextField(
                                        controller: _followupController,
                                        maxLines: 3,
                                        decoration: const InputDecoration(
                                          hintText: 'Add a follow-up note...',
                                          border: InputBorder.none,
                                        ),
                                      ),
                                      const SizedBox(height: 8),
                                      Align(
                                        alignment: Alignment.centerRight,
                                        child: ElevatedButton.icon(
                                          onPressed: _isSubmittingFollowup ? null : _submitFollowupNote,
                                          icon: _isSubmittingFollowup
                                              ? const SizedBox(
                                                  width: 16,
                                                  height: 16,
                                                  child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                                                )
                                              : const Icon(Icons.send, size: 16),
                                          label: const Text('Add Follow-up'),
                                          style: ElevatedButton.styleFrom(
                                            backgroundColor: AppTheme.primaryDark,
                                            foregroundColor: Colors.white,
                                          ),
                                        ),
                                      ),
                                    ],
                                  ),
                                ),
                              ],
                            );
                          },
                        ),
                        const SizedBox(height: 24),

                        // AI Triage & Goodwill Voucher Card
                        Container(
                          width: double.infinity,
                          padding: const EdgeInsets.all(18),
                          decoration: BoxDecoration(
                            gradient: const LinearGradient(
                              colors: [Color(0xFFF5F3FF), Color(0xFFEDE9FE)],
                              begin: Alignment.topLeft,
                              end: Alignment.bottomRight,
                            ),
                            borderRadius: BorderRadius.circular(14),
                            border: Border.all(color: const Color(0xFFDDD6FE)),
                          ),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Row(
                                children: const [
                                  Icon(Icons.smart_toy_outlined, color: Color(0xFF7C3AED)),
                                  SizedBox(width: 8),
                                  Text(
                                    'AI Agent Assessment',
                                    style: TextStyle(fontWeight: FontWeight.bold, color: Color(0xFF6D28D9), fontSize: 15),
                                  ),
                                ],
                              ),
                              const SizedBox(height: 10),
                              Text(
                                'Severity Tier: ${_ticket!.severityTier} | Sentiment: ${_ticket!.sentimentScore.toStringAsFixed(2)}',
                                style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 13, color: Color(0xFF4C1D95)),
                              ),
                              if (_ticket!.aiReasoning != null) ...[
                                const SizedBox(height: 6),
                                Text(
                                  _ticket!.aiReasoning!,
                                  style: const TextStyle(fontSize: 13, color: Color(0xFF5B21B6), height: 1.4),
                                ),
                              ],
                            ],
                          ),
                        ),
                        const SizedBox(height: 24),

                        // Chronological Audit Trace
                        const Text('Resolution Timeline', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: AppTheme.textDark)),
                        const SizedBox(height: 12),
                        if (_ticket!.auditLogs.isEmpty)
                          const Text('No audit events yet.', style: TextStyle(color: AppTheme.textLight))
                        else
                          ListView.separated(
                            shrinkWrap: true,
                            physics: const NeverScrollableScrollPhysics(),
                            itemCount: _ticket!.auditLogs.length,
                            separatorBuilder: (context, index) => const SizedBox(height: 10),
                            itemBuilder: (context, index) {
                              final log = _ticket!.auditLogs[index];
                              return Container(
                                padding: const EdgeInsets.all(12),
                                decoration: BoxDecoration(
                                  color: Colors.white,
                                  borderRadius: BorderRadius.circular(10),
                                  border: Border.all(color: const Color(0xFFE2E8F0)),
                                ),
                                child: Row(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Container(
                                      padding: const EdgeInsets.all(6),
                                      decoration: BoxDecoration(
                                        color: AppTheme.primaryDark.withAlpha(20),
                                        shape: BoxShape.circle,
                                      ),
                                      child: const Icon(Icons.check_circle_outline, size: 16, color: AppTheme.primaryDark),
                                    ),
                                    const SizedBox(width: 10),
                                    Expanded(
                                      child: Column(
                                        crossAxisAlignment: CrossAxisAlignment.start,
                                        children: [
                                          Row(
                                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                            children: [
                                              Text(
                                                log.action,
                                                style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 13, color: AppTheme.textDark),
                                              ),
                                              Text(
                                                '${log.timestamp.hour}:${log.timestamp.minute.toString().padLeft(2, '0')}',
                                                style: const TextStyle(fontSize: 11, color: AppTheme.textLight),
                                              ),
                                            ],
                                          ),
                                          const SizedBox(height: 4),
                                          Text(log.details, style: const TextStyle(fontSize: 12, color: Color(0xFF475569))),
                                        ],
                                      ),
                                    ),
                                  ],
                                ),
                              );
                            },
                          ),
                      ],
                    ),
                  ),
                ),
    );
  }
}
