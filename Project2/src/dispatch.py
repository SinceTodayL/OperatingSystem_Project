import sys
from PyQt5.QtWidgets import QApplication, QWidget, QLabel, QPushButton, QVBoxLayout, QLineEdit
from dataclasses import dataclass

# Dataclass of Page

@dataclass
class Page:
    index: int
    order: int       # for FIFO algorithm
    priority: int    # for LRU algorithm (lower is prior)

method_names = ["FIFO", "LRU"]

# Record the information of OS 
# FIFO, LRU

class Dispatcher:
    """Memory page dispatcher supporting FIFO and LRU algorithms"""
    
    def __init__(self, sum_page_number, dispatch_method="FIFO"): 
        self._page_number = sum_page_number
        self._occupy_page = {}  
        self._occupy_page_num = 0
        self._request_times = 0
        self._fault_times = 0
        self.dispatch_method = dispatch_method

        if dispatch_method not in method_names:
            raise ValueError(f"{dispatch_method} is not accepted.")
        if sum_page_number <= 0:
            raise ValueError(f"Negative page number: {sum_page_number} is not accepted.")

    def _update_occupy_page(self, request_index):
        """Update order and priority for existing pages"""
        for index, value in self._occupy_page.items():
            if index != request_index:
                value.priority += 1
                value.order += 1

    def accept_request(self, request_index):
        """Process page request and handle page faults"""
        self._request_times += 1
        if request_index in self._occupy_page:
            print(f"Page {request_index} already exists.")
            for index, value in self._occupy_page.items():
                if index == request_index:
                    value.priority = 0
                else:
                    value.priority += 1
        else:
            # Page fault occurred
            self._fault_times += 1
            if self._occupy_page_num >= self._page_number:
                print(f"Page fault: {request_index}")
                if self.dispatch_method == "FIFO":
                    self.dispatch_FIFO(request_index)
                elif self.dispatch_method == 'LRU':
                    self.dispatch_LRU(request_index)
            else:
                self._occupy_page_num += 1
                self._occupy_page[request_index] = Page(
                    index=request_index,
                    order=0,
                    priority=0
                )
                self._update_occupy_page(request_index)

    def dispatch_FIFO(self, request_index):
        """Replace page using FIFO algorithm"""
        max_index = max(self._occupy_page, key=lambda i: self._occupy_page[i].order)
        self._occupy_page.pop(max_index)
        self._occupy_page[request_index] = Page(
            index=request_index,
            order=0,
            priority=0
        )
        self._update_occupy_page(request_index)

    def dispatch_LRU(self, request_index):
        """Replace page using LRU algorithm"""
        max_index = max(self._occupy_page, key=lambda i: self._occupy_page[i].priority)
        self._occupy_page.pop(max_index)
        self._occupy_page[request_index] = Page(
            index=request_index,
            order=0,
            priority=0
        )
        self._update_occupy_page(request_index)



