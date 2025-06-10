import sys
from PyQt5.QtWidgets import (QApplication, QWidget, QLabel, QPushButton, QVBoxLayout, 
                             QHBoxLayout, QGridLayout, QGroupBox, QProgressBar, 
                             QComboBox, QSpinBox, QTextEdit, QFrame, QTableWidget, 
                             QTableWidgetItem, QHeaderView)
from PyQt5.QtCore import QTimer, Qt
from PyQt5.QtGui import QFont, QColor
from allocation import MemoryDispatch, Allocation
from dispatch import Dispatcher

class MemoryPageWidget(QFrame):
    """Memory page widget displaying instructions in table format"""
    
    def __init__(self, page_id):
        super().__init__()
        self.page_id = page_id
        self.setFixedSize(250, 160)
        self.setStyleSheet("border: 2px solid #333; margin: 5px; background: white; border-radius: 5px;")
        self.setup_ui()
        
    def setup_ui(self):
        layout = QVBoxLayout()
        layout.setSpacing(5)
        
        # Page title
        self.title_label = QLabel(f"📄 页面 {self.page_id}")
        self.title_label.setAlignment(Qt.AlignCenter)
        self.title_label.setStyleSheet("font-weight: bold; font-size: 14px; color: #2196F3; padding: 5px;")
        layout.addWidget(self.title_label)
        
        # Instruction table (2x5 for 10 instructions)
        self.table = QTableWidget(2, 5)
        self.table.setFixedSize(220, 80)
        self.table.horizontalHeader().setVisible(False)
        self.table.verticalHeader().setVisible(False)
        self.table.horizontalHeader().setSectionResizeMode(QHeaderView.Stretch)
        self.table.verticalHeader().setSectionResizeMode(QHeaderView.Stretch)
        self.table.setStyleSheet("""
            QTableWidget {
                gridline-color: #cccccc;
                border: 1px solid #cccccc;
                border-radius: 3px;
            }
            QTableWidget::item {
                text-align: center;
                font-weight: bold;
                font-size: 11px;
            }
        """)
        layout.addWidget(self.table)
        
        # Status label
        self.status_label = QLabel("🔘 未加载")
        self.status_label.setAlignment(Qt.AlignCenter)
        self.status_label.setStyleSheet("color: gray; font-size: 12px; padding: 3px;")
        layout.addWidget(self.status_label)
        
        self.setLayout(layout)
        
    def set_page_content(self, page_id):
        """Set page content and display instructions"""
        self.page_id = page_id
        self.title_label.setText(f"📄 页面 {page_id}")
        
        # Fill table with consecutive instructions for this page
        start_inst = page_id * 10
        for row in range(2):
            for col in range(5):
                index = row * 5 + col
                instruction_num = start_inst + index
                item = QTableWidgetItem(str(instruction_num))
                item.setTextAlignment(Qt.AlignCenter)
                self.table.setItem(row, col, item)
    
    def set_empty(self):
        """Set as empty frame"""
        self.page_id = -1
        self.title_label.setText("🔘 空页框")
        self.status_label.setText("🔘 未使用")
        self.status_label.setStyleSheet("color: gray; font-size: 12px; padding: 3px;")
        self.setStyleSheet("border: 2px dashed #999; margin: 5px; background: #f9f9f9; border-radius: 5px;")
        
        # Clear table
        for row in range(2):
            for col in range(5):
                item = QTableWidgetItem("--")
                item.setTextAlignment(Qt.AlignCenter)
                item.setBackground(QColor(245, 245, 245))
                self.table.setItem(row, col, item)
        
    def set_loaded(self, is_loaded=True):
        """Set loading status"""
        if is_loaded:
            self.setStyleSheet("border: 2px solid #4CAF50; margin: 5px; background: #f0fff0; border-radius: 5px;")
            self.status_label.setText("✅ 已加载到内存")
            self.status_label.setStyleSheet("color: #4CAF50; font-size: 12px; font-weight: bold; padding: 3px;")
            # Set table items to green background
            for row in range(2):
                for col in range(5):
                    item = self.table.item(row, col)
                    if item:
                        item.setBackground(QColor(200, 230, 201))
        else:
            self.setStyleSheet("border: 2px solid #333; margin: 5px; background: white; border-radius: 5px;")
            self.status_label.setText("🔘 未加载")
            self.status_label.setStyleSheet("color: gray; font-size: 12px; padding: 3px;")
            # Reset table item colors
            for row in range(2):
                for col in range(5):
                    item = self.table.item(row, col)
                    if item:
                        item.setBackground(QColor(255, 255, 255))
            
    def highlight_instruction(self, instruction_index):
        """Highlight currently executing instruction"""
        page_start = self.page_id * 10
        if page_start <= instruction_index < page_start + 10:
            self.setStyleSheet("border: 3px solid #FF5722; margin: 5px; background: #fff3e0; border-radius: 5px;")
            self.status_label.setText(f"⚡ 执行指令 {instruction_index}")
            self.status_label.setStyleSheet("color: #FF5722; font-weight: bold; font-size: 12px; padding: 3px;")
            
            # Highlight corresponding table cell
            local_index = instruction_index - page_start
            row = local_index // 5
            col = local_index % 5
            item = self.table.item(row, col)
            if item:
                item.setBackground(QColor(255, 193, 7))

class PagingUI(QWidget):
    """Main UI class for memory paging visualization"""
    
    def __init__(self):
        super().__init__()
        self.memory_dispatch = None
        self.memory_widgets = {}
        self.frame_to_page = {}  # Frame ID to page ID mapping
        self.page_to_frame = {}  # Page ID to frame ID mapping
        self.auto_timer = QTimer()
        self.auto_timer.timeout.connect(self.step_execute)
        self.execution_finished = False
        self.initUI()
        
    def initUI(self):
        self.setWindowTitle("🖥️ 内存页面调度算法可视化")
        self.setGeometry(50, 50, 1400, 800)
        self.setStyleSheet("""
            QWidget {
                background-color: #f5f5f5;
                font-family: 'Microsoft YaHei', Arial, sans-serif;
            }
            QGroupBox {
                font-weight: bold;
                font-size: 13px;
                border: 2px solid #2196F3;
                border-radius: 8px;
                margin-top: 15px;
                padding-top: 10px;
                background-color: white;
                color: #2196F3;
            }
            QGroupBox::title {
                subcontrol-origin: margin;
                left: 15px;
                padding: 0 10px 0 10px;
                background-color: white;
            }
        """)
        
        main_layout = QHBoxLayout()
        
        # Left panel - memory page display
        left_panel = self.create_left_panel()
        main_layout.addWidget(left_panel, 7)
        
        # Right panel - logs and controls
        right_panel = self.create_right_panel()
        main_layout.addWidget(right_panel, 3)
        
        self.setLayout(main_layout)
        
    def create_left_panel(self):
        """Create left panel with status and memory display"""
        panel = QWidget()
        layout = QVBoxLayout()
        
        # Current status
        status_group = QGroupBox("📊 当前执行状态")
        status_layout = QVBoxLayout()
        
        self.current_inst_label = QLabel("📍 当前指令: --")
        self.current_inst_label.setStyleSheet("font-size: 14px; color: #1976D2; padding: 3px;")
        
        self.current_page_label = QLabel("📄 当前页面: --")
        self.current_page_label.setStyleSheet("font-size: 14px; color: #1976D2; padding: 3px;")
        
        self.fault_label = QLabel("✅ 页面不命中: 否")
        self.fault_label.setStyleSheet("font-size: 14px; color: #4CAF50; padding: 3px;")
        
        status_layout.addWidget(self.current_inst_label)
        status_layout.addWidget(self.current_page_label)
        status_layout.addWidget(self.fault_label)
        status_group.setLayout(status_layout)
        layout.addWidget(status_group)
        
        # Memory page display
        memory_group = QGroupBox("💾 内存中的页面 (表格显示)")
        memory_layout = QVBoxLayout()
        
        self.memory_container = QWidget()
        self.memory_grid = QGridLayout()
        self.memory_container.setLayout(self.memory_grid)
        memory_layout.addWidget(self.memory_container)
        
        memory_group.setLayout(memory_layout)
        layout.addWidget(memory_group)
        
        panel.setLayout(layout)
        return panel
        
    def create_right_panel(self):
        """Create right panel with logs and controls"""
        panel = QWidget()
        layout = QVBoxLayout()
        
        # Execution log
        log_group = QGroupBox("📝 执行日志")
        log_layout = QVBoxLayout()
        
        self.log_text = QTextEdit()
        self.log_text.setMaximumHeight(400)
        self.log_text.setStyleSheet("""
            QTextEdit {
                border: 1px solid #cccccc;
                border-radius: 5px;
                background-color: white;
                font-family: 'Consolas', monospace;
                font-size: 11px;
                padding: 8px;
            }
        """)
        log_layout.addWidget(self.log_text)
        
        log_group.setLayout(log_layout)
        layout.addWidget(log_group, 3)
        
        # Control panel
        control_group = QGroupBox("🎛️ 控制面板")
        control_layout = QVBoxLayout()
        
        # Parameter settings
        params_layout = QGridLayout()
        
        params_layout.addWidget(QLabel("📝 指令数:"), 0, 0)
        self.inst_spin = QSpinBox()
        self.inst_spin.setRange(100, 500)
        self.inst_spin.setValue(320)
        self.inst_spin.setStyleSheet("padding: 3px; border: 1px solid #ccc; border-radius: 3px;")
        params_layout.addWidget(self.inst_spin, 0, 1)
        
        params_layout.addWidget(QLabel("💾 内存页框:"), 1, 0)
        self.memory_spin = QSpinBox()
        self.memory_spin.setRange(3, 8)
        self.memory_spin.setValue(4)
        self.memory_spin.setStyleSheet("padding: 3px; border: 1px solid #ccc; border-radius: 3px;")
        params_layout.addWidget(self.memory_spin, 1, 1)
        
        params_layout.addWidget(QLabel("⚙️ 算法:"), 2, 0)
        self.algo_combo = QComboBox()
        self.algo_combo.addItems(["FIFO", "LRU"])
        self.algo_combo.setStyleSheet("padding: 3px; border: 1px solid #ccc; border-radius: 3px;")
        params_layout.addWidget(self.algo_combo, 2, 1)
        
        control_layout.addLayout(params_layout)
        
        # Button styles
        button_style = """
            QPushButton {
                font-size: 12px;
                font-weight: bold;
                padding: 8px 15px;
                border: none;
                border-radius: 6px;
                color: black;
                min-height: 30px;
            }
            QPushButton:hover {
                opacity: 0.8;
            }
            QPushButton:pressed {
                opacity: 0.6;
            }
            QPushButton:disabled {
                background-color: #cccccc;
                color: #666666;
            }
        """
        
        # Control buttons
        self.start_btn = QPushButton("🚀 开始模拟")
        self.start_btn.setStyleSheet(button_style + "background-color: #4CAF50;")
        self.start_btn.clicked.connect(self.start_simulation)
        control_layout.addWidget(self.start_btn)
        
        self.step_btn = QPushButton("👆 逐步执行")
        self.step_btn.setStyleSheet(button_style + "background-color: #2196F3;")
        self.step_btn.clicked.connect(self.step_execute)
        self.step_btn.setEnabled(False)
        control_layout.addWidget(self.step_btn)
        
        self.auto_btn = QPushButton("⚡ 连续执行")
        self.auto_btn.setStyleSheet(button_style + "background-color: #FF9800;")
        self.auto_btn.clicked.connect(self.auto_execute)
        self.auto_btn.setEnabled(False)
        control_layout.addWidget(self.auto_btn)
        
        self.pause_btn = QPushButton("⏸️ 暂停")
        self.pause_btn.setStyleSheet(button_style + "background-color: #FF5722;")
        self.pause_btn.clicked.connect(self.pause_execute)
        self.pause_btn.setEnabled(False)
        control_layout.addWidget(self.pause_btn)
        
        self.reset_btn = QPushButton("🔄 重置")
        self.reset_btn.setStyleSheet(button_style + "background-color: #607D8B;")
        self.reset_btn.clicked.connect(self.reset_simulation)
        control_layout.addWidget(self.reset_btn)
        
        # Progress and statistics
        self.progress_bar = QProgressBar()
        self.progress_bar.setStyleSheet("""
            QProgressBar {
                border: 2px solid #2196F3;
                border-radius: 5px;
                text-align: center;
                font-weight: bold;
                height: 25px;
            }
            QProgressBar::chunk {
                background-color: #4CAF50;
                border-radius: 3px;
            }
        """)
        control_layout.addWidget(self.progress_bar)
        
        self.stats_label = QLabel("📊 请求: 0 | 不命中: 0 | 不命中率: 0%")
        self.stats_label.setStyleSheet("font-size: 11px; color: #666; padding: 3px;")
        control_layout.addWidget(self.stats_label)
        
        control_group.setLayout(control_layout)
        layout.addWidget(control_group, 2)
        
        panel.setLayout(layout)
        return panel
        
    def start_simulation(self):
        """Initialize and start simulation"""
        inst_num = self.inst_spin.value()
        memory_size = self.memory_spin.value()
        algorithm = self.algo_combo.currentText()
        
        # Create dispatcher with correct parameters
        self.memory_dispatch = MemoryDispatch()
        self.memory_dispatch.dispatcher = Dispatcher(sum_page_number=memory_size, dispatch_method=algorithm)
        self.memory_dispatch.allocation = Allocation(order_nums=inst_num, request_order_nums=inst_num)
        self.memory_dispatch.start()
        
        self.execution_finished = False
        
        # Clear and initialize display
        self.clear_memory_display()
        self.init_memory_frames(memory_size)
        
        # Set UI state
        self.progress_bar.setMaximum(inst_num)
        self.progress_bar.setValue(0)
        
        self.start_btn.setEnabled(False)
        self.step_btn.setEnabled(True)
        self.auto_btn.setEnabled(True)
        self.pause_btn.setEnabled(False)
        
        # Initialize log
        self.log_text.clear()
        self.log_text.append(f"🚀 开始模拟 - 算法: {algorithm}, 内存: {memory_size}页框, 指令: {inst_num}条")
        self.log_text.append("=" * 50)

    def init_memory_frames(self, frame_count):
        """Initialize memory frame display"""
        self.memory_frame_count = frame_count
        self.frame_to_page = {}
        self.page_to_frame = {}
        
        # Create fixed number of frame displays
        for i in range(frame_count):
            widget = MemoryPageWidget(-1)
            widget.set_empty()
            self.memory_widgets[f"frame_{i}"] = widget
            self.frame_to_page[i] = None
            
            row = i // 2
            col = i % 2
            self.memory_grid.addWidget(widget, row, col)

    def step_execute(self):
        """Execute one step of simulation"""
        if not self.memory_dispatch or self.execution_finished:
            return
            
        allocation = self.memory_dispatch.allocation
        dispatcher = self.memory_dispatch.dispatcher
        
        # Check if completed
        if allocation.cur_index >= len(allocation.order_seq):
            self.finish_simulation()
            return
            
        current_index = allocation.cur_index
        current_instruction = allocation.order_seq[current_index]
        current_page = current_instruction // 10
        
        # Record state before execution
        old_fault_count = dispatcher._fault_times
        old_memory_pages = set(dispatcher._occupy_page.keys())
        
        # Execute instruction
        dispatcher.accept_request(current_page)
        
        # Check page miss
        page_fault = dispatcher._fault_times > old_fault_count
        new_memory_pages = set(dispatcher._occupy_page.keys())
        
        # Record index before moving to check completion
        old_index = allocation.cur_index
        
        # Move to next instruction
        allocation.go_next()
        
        # Update frame mapping if page fault occurred
        if page_fault:
            self.update_frame_mapping(old_memory_pages, new_memory_pages, current_page)
        
        # Update display
        self.update_display(current_instruction, current_page, page_fault)
        self.update_memory_display()
        
        # Log execution
        step_num = current_index + 1
        status = "页面不命中" if page_fault else "页面命中"
        memory_pages = sorted(list(dispatcher._occupy_page.keys()))
        
        self.log_text.append(f"步骤{step_num}: 指令{current_instruction} -> 页面{current_page} ({status})")
        self.log_text.append(f"💾 内存: {memory_pages}")
        
        # Check if reached end
        if old_index == allocation.cur_index:
            # Ensure progress bar reaches 100%
            self.progress_bar.setValue(len(allocation.order_seq))
            self.finish_simulation()

    def update_frame_mapping(self, old_pages, new_pages, new_page):
        """Update frame to page mapping relationships"""
        # Find replaced pages
        removed_pages = old_pages - new_pages
        added_pages = new_pages - old_pages
        
        if removed_pages and added_pages:
            # Page replacement occurred
            removed_page = list(removed_pages)[0]
            added_page = list(added_pages)[0]
            
            # Find frame of replaced page
            if removed_page in self.page_to_frame:
                frame_id = self.page_to_frame[removed_page]
                
                # Update mapping
                del self.page_to_frame[removed_page]
                self.frame_to_page[frame_id] = added_page
                self.page_to_frame[added_page] = frame_id
                
                self.log_text.append(f"   🔄 页框{frame_id}: 页面{removed_page} → 页面{added_page}")
        
        elif added_pages and not removed_pages:
            # New page loaded to empty frame
            added_page = list(added_pages)[0]
            
            # Find first empty frame
            for frame_id in range(self.memory_frame_count):
                if self.frame_to_page[frame_id] is None:
                    self.frame_to_page[frame_id] = added_page
                    self.page_to_frame[added_page] = frame_id
                    self.log_text.append(f"   ➕ 页框{frame_id}: 加载页面{added_page}")
                    break
            
    def auto_execute(self):
        """Start continuous execution"""
        self.auto_timer.start(100)
        self.auto_btn.setEnabled(False)
        self.step_btn.setEnabled(False)
        self.pause_btn.setEnabled(True)
        
    def pause_execute(self):
        """Pause execution"""
        self.auto_timer.stop()
        self.auto_btn.setEnabled(True)
        self.step_btn.setEnabled(True)
        self.pause_btn.setEnabled(False)
        
    def finish_simulation(self):
        """Complete simulation"""
        self.execution_finished = True
        self.auto_timer.stop()
        
        if self.memory_dispatch:
            self.memory_dispatch.finish()
        
        # Update button states
        self.step_btn.setEnabled(False)
        self.auto_btn.setEnabled(False)
        self.pause_btn.setEnabled(False)
        self.start_btn.setEnabled(True)
        
        # Display final statistics
        dispatcher = self.memory_dispatch.dispatcher
        if dispatcher._request_times > 0:
            fault_rate = dispatcher._fault_times / dispatcher._request_times * 100
            self.log_text.append("=" * 50)
            self.log_text.append("🎉 模拟完成!")
            self.log_text.append(f"📊 总请求: {dispatcher._request_times}")
            self.log_text.append(f"❌ 页面不命中: {dispatcher._fault_times}")
            self.log_text.append(f"📈 不命中率: {fault_rate:.2f}%")
            
    def reset_simulation(self):
        """Reset simulation to initial state"""
        self.auto_timer.stop()
        self.memory_dispatch = None
        self.execution_finished = False
        self.frame_to_page = {}
        self.page_to_frame = {}
        
        # Reset UI state
        self.start_btn.setEnabled(True)
        self.step_btn.setEnabled(False)
        self.auto_btn.setEnabled(False)
        self.pause_btn.setEnabled(False)
        
        # Clear display
        self.log_text.clear()
        self.progress_bar.setValue(0)
        self.current_inst_label.setText("📍 当前指令: --")
        self.current_page_label.setText("📄 当前页面: --")
        self.fault_label.setText("✅ 页面不命中: 否")
        self.stats_label.setText("📊 请求: 0 | 不命中: 0 | 不命中率: 0%")
        
        self.clear_memory_display()
        
    def update_display(self, current_instruction, current_page, page_fault):
        """Update status display"""
        self.current_inst_label.setText(f"📍 当前指令: {current_instruction}")
        self.current_page_label.setText(f"📄 当前页面: {current_page}")
        
        if page_fault:
            self.fault_label.setText("❌ 页面不命中: 是")
            self.fault_label.setStyleSheet("font-size: 14px; color: #F44336; padding: 3px;")
        else:
            self.fault_label.setText("✅ 页面不命中: 否")
            self.fault_label.setStyleSheet("font-size: 14px; color: #4CAF50; padding: 3px;")
        
        # Update progress
        if self.memory_dispatch:
            current_index = self.memory_dispatch.allocation.cur_index
            self.progress_bar.setValue(current_index)
            
            # Update statistics
            dispatcher = self.memory_dispatch.dispatcher
            if dispatcher._request_times > 0:
                fault_rate = dispatcher._fault_times / dispatcher._request_times * 100
                self.stats_label.setText(f"📊 请求: {dispatcher._request_times} | "
                                       f"不命中: {dispatcher._fault_times} | "
                                       f"不命中率: {fault_rate:.1f}%")
        
    def update_memory_display(self):
        """Update memory frame display based on mapping"""
        if not self.memory_dispatch:
            return
            
        current_instruction = None
        
        # Get current instruction for highlighting
        allocation = self.memory_dispatch.allocation
        if allocation.cur_index > 0 and allocation.cur_index <= len(allocation.order_seq):
            current_instruction = allocation.order_seq[allocation.cur_index - 1]
        
        # Update display according to frame mapping
        for frame_id in range(self.memory_frame_count):
            frame_key = f"frame_{frame_id}"
            widget = self.memory_widgets[frame_key]
            page_id = self.frame_to_page[frame_id]
            
            if page_id is not None:
                # Frame has page
                widget.set_page_content(page_id)
                widget.set_loaded(True)
                
                # Highlight current instruction
                if current_instruction is not None:
                    widget.highlight_instruction(current_instruction)
            else:
                # Frame is empty
                widget.set_empty()
                
    def clear_memory_display(self):
        """Clear memory display"""
        for widget in self.memory_widgets.values():
            self.memory_grid.removeWidget(widget)
            widget.setParent(None)
        self.memory_widgets.clear()

if __name__ == '__main__':
    app = QApplication(sys.argv)
    ui = PagingUI()
    ui.show()
    sys.exit(app.exec_())