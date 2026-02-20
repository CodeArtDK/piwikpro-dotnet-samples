// Live Map D3 Visualization
window.liveMap = {
    svg: null,
    simulation: null,
    dotnetRef: null,
    container: null,
    nodes: [],
    links: [],
    highwayLinks: [], // Dotted lines for Next Pages highways
    nodeElements: null,
    linkElements: null,
    highwayLinkElements: null,
    trafficDots: [],
    orbitingDots: new Map(), // Persistent orbiting dots per node
    animationInterval: null,
    orbitAnimationFrame: null,
    zoom: null,
    width: 0,
    height: 0,
    selectedNodeId: null,
    maxTraffic: 100, // Will be updated based on actual data

    initialize: function (containerId, dotnetReference) {
        // Check if D3 is available
        if (typeof d3 === 'undefined') {
            console.error('D3.js is not loaded. Please ensure D3 is loaded before livemap.js');
            return;
        }

        this.dotnetRef = dotnetReference;
        this.container = document.getElementById(containerId);

        if (!this.container) {
            console.error('Live map container not found:', containerId);
            return;
        }

        // Clear any existing content
        this.container.innerHTML = '';

        // Get container dimensions
        this.width = this.container.clientWidth;
        this.height = this.container.clientHeight || 600;

        // Create SVG
        this.svg = d3.select(`#${containerId}`)
            .append('svg')
            .attr('width', '100%')
            .attr('height', '100%')
            .attr('viewBox', `0 0 ${this.width} ${this.height}`)
            .style('background', '#f8f9fa');

        // Add zoom behavior
        this.zoom = d3.zoom()
            .scaleExtent([0.3, 3])
            .on('zoom', (event) => {
                this.mainGroup.attr('transform', event.transform);
            });

        this.svg.call(this.zoom);

        // Main group for all elements
        this.mainGroup = this.svg.append('g');

        // Create groups for links, highways, nodes, and traffic
        this.linksGroup = this.mainGroup.append('g').attr('class', 'links');
        this.highwaysGroup = this.mainGroup.append('g').attr('class', 'highways');
        this.nodesGroup = this.mainGroup.append('g').attr('class', 'nodes');
        this.trafficGroup = this.mainGroup.append('g').attr('class', 'traffic');

        // Initialize force simulation
        this.simulation = d3.forceSimulation()
            .force('link', d3.forceLink().id(d => d.id).distance(120).strength(0.5))
            .force('charge', d3.forceManyBody().strength(-400))
            .force('center', d3.forceCenter(this.width / 2, this.height / 2))
            .force('collision', d3.forceCollide().radius(d => this.getNodeRadius(d) + 10))
            .on('tick', () => this.tick());

        // Start traffic animation
        this.startTrafficAnimation();
        this.startOrbitAnimation();

        console.log('Live map initialized');
    },

    updateData: function (nodesData, linksData) {
        if (!this.svg) {
            console.error('SVG not initialized');
            return;
        }

        // Calculate max traffic for proportional sizing
        this.maxTraffic = Math.max(100, ...nodesData.map(n => n.traffic || 0));

        this.nodes = nodesData.map(n => ({
            ...n,
            x: n.x || this.width / 2 + (Math.random() - 0.5) * 200,
            y: n.y || this.height / 2 + (Math.random() - 0.5) * 200
        }));

        this.links = linksData.map(l => ({
            source: l.source,
            target: l.target,
            traffic: l.traffic || 0
        }));

        this.render();
        
        // Update orbiting dots for nodes with traffic
        this.updateOrbitingDots();
    },

    render: function () {
        // Update links
        this.linkElements = this.linksGroup.selectAll('.link')
            .data(this.links, d => `${d.source}-${d.target}`);

        this.linkElements.exit().remove();

        const linkEnter = this.linkElements.enter()
            .append('line')
            .attr('class', 'link')
            .attr('stroke', '#999')
            .attr('stroke-opacity', 0.6)
            .attr('stroke-width', d => Math.max(1, Math.min(5, d.traffic / 10)));

        this.linkElements = linkEnter.merge(this.linkElements);

        // Update highway links (dotted lines for Next Pages)
        this.highwayLinkElements = this.highwaysGroup.selectAll('.highway-link')
            .data(this.highwayLinks, d => `${d.source}-${d.target}`);

        this.highwayLinkElements.exit().remove();

        const highwayEnter = this.highwayLinkElements.enter()
            .append('line')
            .attr('class', 'highway-link')
            .attr('stroke', '#fb8c00')
            .attr('stroke-opacity', 0.8)
            .attr('stroke-width', 2)
            .attr('stroke-dasharray', '5,5');

        this.highwayLinkElements = highwayEnter.merge(this.highwayLinkElements);

        // Update nodes
        this.nodeElements = this.nodesGroup.selectAll('.node')
            .data(this.nodes, d => d.id);

        this.nodeElements.exit().remove();

        const nodeEnter = this.nodeElements.enter()
            .append('g')
            .attr('class', 'node')
            .call(this.drag())
            .on('click', (event, d) => this.onNodeClick(event, d))
            .on('dblclick', (event, d) => this.onNodeDoubleClick(event, d));

        // Node circle with traffic-based coloring and sizing
        nodeEnter.append('circle')
            .attr('r', d => this.getNodeRadius(d))
            .attr('fill', d => this.getNodeColor(d))
            .attr('stroke', d => this.getNodeStroke(d))
            .attr('stroke-width', d => this.getNodeStrokeWidth(d))
            .style('cursor', 'pointer');

        // Special indicator ring for Start node
        nodeEnter.filter(d => d.id === 'root')
            .append('circle')
            .attr('class', 'start-indicator')
            .attr('r', d => this.getNodeRadius(d) + 8)
            .attr('fill', 'none')
            .attr('stroke', '#0d47a1')
            .attr('stroke-width', 3)
            .attr('stroke-dasharray', '4,2');

        // Traffic count text
        nodeEnter.append('text')
            .attr('class', 'node-count')
            .attr('text-anchor', 'middle')
            .attr('dy', '0.35em')
            .attr('fill', '#fff')
            .attr('font-weight', 'bold')
            .attr('font-size', d => d.id === 'root' ? '16px' : '14px')
            .style('pointer-events', 'none')
            .text(d => d.traffic || 0);

        // Node label
        nodeEnter.append('text')
            .attr('class', 'node-label')
            .attr('text-anchor', 'middle')
            .attr('dy', d => this.getNodeRadius(d) + 15)
            .attr('fill', '#333')
            .attr('font-size', d => d.id === 'root' ? '14px' : '12px')
            .attr('font-weight', d => d.id === 'root' ? 'bold' : 'normal')
            .style('pointer-events', 'none')
            .text(d => this.truncateText(d.title || d.id, 20));

        this.nodeElements = nodeEnter.merge(this.nodeElements);

        // Update existing nodes
        this.nodeElements.select('circle:not(.start-indicator)')
            .attr('r', d => this.getNodeRadius(d))
            .attr('fill', d => this.getNodeColor(d))
            .attr('stroke', d => this.getNodeStroke(d))
            .attr('stroke-width', d => this.getNodeStrokeWidth(d));

        // Update start indicator position
        this.nodeElements.select('.start-indicator')
            .attr('r', d => this.getNodeRadius(d) + 8);

        this.nodeElements.select('.node-count')
            .text(d => d.traffic || 0);

        this.nodeElements.select('.node-label')
            .attr('dy', d => this.getNodeRadius(d) + 15)
            .text(d => this.truncateText(d.title || d.id, 20));

        // Update simulation
        this.simulation.nodes(this.nodes);
        this.simulation.force('link').links(this.links);
        this.simulation.force('collision').radius(d => this.getNodeRadius(d) + 10);
        this.simulation.alpha(0.3).restart();
    },

    getNodeStroke: function (node) {
        if (node.id === this.selectedNodeId) return '#0d47a1';
        if (node.id === 'root') return '#1565c0';
        return '#fff';
    },

    getNodeStrokeWidth: function (node) {
        if (node.id === this.selectedNodeId) return 4;
        if (node.id === 'root') return 3;
        return 2;
    },

    getNodeRadius: function (node) {
        const traffic = node.traffic || 0;
        const minRadius = 25;
        const maxRadius = 55;
        
        // Start node gets a slightly larger minimum
        if (node.id === 'root') {
            const baseRadius = Math.max(minRadius + 5, minRadius + (traffic / this.maxTraffic) * (maxRadius - minRadius));
            return Math.min(maxRadius + 5, baseRadius);
        }
        
        // Proportional sizing based on traffic relative to max
        const ratio = Math.min(1, traffic / this.maxTraffic);
        return minRadius + ratio * (maxRadius - minRadius);
    },

    getNodeColor: function (node) {
        const traffic = node.traffic || 0;
        if (traffic >= 30) return '#43a047'; // High - green
        if (traffic >= 15) return '#fb8c00'; // Medium - orange
        return '#757575'; // Low - gray
    },

    tick: function () {
        if (this.linkElements) {
            this.linkElements
                .attr('x1', d => d.source.x)
                .attr('y1', d => d.source.y)
                .attr('x2', d => d.target.x)
                .attr('y2', d => d.target.y);
        }

        if (this.highwayLinkElements) {
            this.highwayLinkElements
                .attr('x1', d => {
                    const sourceNode = this.nodes.find(n => n.id === d.source);
                    return sourceNode ? sourceNode.x : 0;
                })
                .attr('y1', d => {
                    const sourceNode = this.nodes.find(n => n.id === d.source);
                    return sourceNode ? sourceNode.y : 0;
                })
                .attr('x2', d => {
                    const targetNode = this.nodes.find(n => n.id === d.target);
                    return targetNode ? targetNode.x : 0;
                })
                .attr('y2', d => {
                    const targetNode = this.nodes.find(n => n.id === d.target);
                    return targetNode ? targetNode.y : 0;
                });
        }

        if (this.nodeElements) {
            this.nodeElements.attr('transform', d => `translate(${d.x},${d.y})`);
        }
    },

    drag: function () {
        return d3.drag()
            .on('start', (event, d) => {
                if (!event.active) this.simulation.alphaTarget(0.3).restart();
                d.fx = d.x;
                d.fy = d.y;
            })
            .on('drag', (event, d) => {
                d.fx = event.x;
                d.fy = event.y;
            })
            .on('end', (event, d) => {
                if (!event.active) this.simulation.alphaTarget(0);
                d.fx = null;
                d.fy = null;
            });
    },

    onNodeClick: function (event, node) {
        event.stopPropagation();
        this.selectedNodeId = node.id;

        // Update visual selection
        this.nodeElements.select('circle:not(.start-indicator)')
            .attr('stroke', d => this.getNodeStroke(d))
            .attr('stroke-width', d => this.getNodeStrokeWidth(d));

        // Notify Blazor
        if (this.dotnetRef) {
            this.dotnetRef.invokeMethodAsync('OnNodeSelected', node.id, node.url, node.title);
        }
    },

    onNodeDoubleClick: function (event, node) {
        event.stopPropagation();
        if (this.dotnetRef) {
            this.dotnetRef.invokeMethodAsync('OnNodeDoubleClick', node.id, node.expanded);
        }
    },

    startTrafficAnimation: function () {
        if (this.animationInterval) {
            clearInterval(this.animationInterval);
        }

        this.animationInterval = setInterval(() => {
            this.animateTraffic();
        }, 2000);
    },

    animateTraffic: function () {
        if (!this.links || this.links.length === 0) return;

        // Create traffic dots for random links based on traffic volume
        this.links.forEach(link => {
            if (Math.random() < (link.traffic || 1) / 50) {
                this.createTrafficDot(link);
            }
        });
    },

    createTrafficDot: function (link) {
        if (!this.trafficGroup) return;

        const sourceNode = this.nodes.find(n => n.id === (link.source.id || link.source));
        const targetNode = this.nodes.find(n => n.id === (link.target.id || link.target));

        if (!sourceNode || !targetNode) return;

        const dot = this.trafficGroup.append('circle')
            .attr('class', 'traffic-dot')
            .attr('r', 4)
            .attr('fill', '#2196f3')
            .attr('cx', sourceNode.x)
            .attr('cy', sourceNode.y);

        dot.transition()
            .duration(1500)
            .ease(d3.easeLinear)
            .attr('cx', targetNode.x)
            .attr('cy', targetNode.y)
            .on('end', function () {
                d3.select(this).remove();
            });
    },

    updateTraffic: function (nodeTraffic) {
        if (!this.nodes) return;

        // Update max traffic for proportional sizing
        const newMaxTraffic = Math.max(100, ...nodeTraffic.map(nt => nt.traffic || 0));
        if (newMaxTraffic > this.maxTraffic) {
            this.maxTraffic = newMaxTraffic;
        }

        nodeTraffic.forEach(nt => {
            const node = this.nodes.find(n => n.id === nt.nodeId);
            if (node) {
                node.traffic = nt.traffic;
            }
        });

        // Update node visuals
        if (this.nodeElements) {
            this.nodeElements.select('circle:not(.start-indicator)')
                .attr('r', d => this.getNodeRadius(d))
                .attr('fill', d => this.getNodeColor(d));

            this.nodeElements.select('.start-indicator')
                .attr('r', d => this.getNodeRadius(d) + 8);

            this.nodeElements.select('.node-count')
                .text(d => d.traffic || 0);

            this.nodeElements.select('.node-label')
                .attr('dy', d => this.getNodeRadius(d) + 15);
        }

        // Update orbiting dots
        this.updateOrbitingDots();
    },

    // Update highways (dotted lines showing typical navigation paths)
    updateHighways: function (highways) {
        if (!highways || !Array.isArray(highways)) {
            this.highwayLinks = [];
        } else {
            // Filter to only include highways between visible nodes
            this.highwayLinks = highways.filter(h => {
                const sourceExists = this.nodes.find(n => n.id === h.source);
                const targetExists = this.nodes.find(n => n.id === h.target);
                // Check it's not already a regular link
                const alreadyLinked = this.links.find(l => 
                    (l.source.id || l.source) === h.source && (l.target.id || l.target) === h.target
                );
                return sourceExists && targetExists && !alreadyLinked;
            });
        }
        this.render();
    },

    // Manage persistent orbiting dots
    updateOrbitingDots: function () {
        this.nodes.forEach(node => {
            const traffic = node.traffic || 0;
            // Number of orbiting dots based on traffic
            const dotCount = Math.min(5, Math.floor(traffic / 10) + (traffic > 0 ? 1 : 0));
            
            if (!this.orbitingDots.has(node.id)) {
                this.orbitingDots.set(node.id, []);
            }
            
            const currentDots = this.orbitingDots.get(node.id);
            
            // Add more dots if needed
            while (currentDots.length < dotCount) {
                const dot = this.createPersistentOrbitDot(node, currentDots.length);
                if (dot) currentDots.push(dot);
            }
            
            // Remove excess dots
            while (currentDots.length > dotCount) {
                const dot = currentDots.pop();
                if (dot && dot.element) {
                    dot.element.remove();
                }
            }
        });
    },

    createPersistentOrbitDot: function (node, index) {
        if (!this.trafficGroup || !node.x || !node.y) return null;

        const radius = this.getNodeRadius(node) + 12;
        const startAngle = (index * Math.PI * 2 / 5) + Math.random() * 0.5;
        
        const element = this.trafficGroup.append('circle')
            .attr('class', 'persistent-orbit-dot')
            .attr('r', 4)
            .attr('fill', '#2196f3')
            .attr('opacity', 0.9)
            .attr('cx', node.x + Math.cos(startAngle) * radius)
            .attr('cy', node.y + Math.sin(startAngle) * radius);

        return {
            element: element,
            nodeId: node.id,
            angle: startAngle,
            radius: radius,
            speed: 0.02 + Math.random() * 0.01 // Radians per frame
        };
    },

    startOrbitAnimation: function () {
        if (this.orbitAnimationFrame) {
            cancelAnimationFrame(this.orbitAnimationFrame);
        }

        const animate = () => {
            this.animateOrbitingDots();
            this.orbitAnimationFrame = requestAnimationFrame(animate);
        };
        this.orbitAnimationFrame = requestAnimationFrame(animate);
    },

    animateOrbitingDots: function () {
        this.orbitingDots.forEach((dots, nodeId) => {
            const node = this.nodes.find(n => n.id === nodeId);
            if (!node || !node.x || !node.y) return;

            dots.forEach(dot => {
                if (!dot || !dot.element) return;
                
                // Update angle
                dot.angle += dot.speed;
                if (dot.angle > Math.PI * 2) dot.angle -= Math.PI * 2;
                
                // Update radius based on current node size
                dot.radius = this.getNodeRadius(node) + 12;
                
                // Update position
                const x = node.x + Math.cos(dot.angle) * dot.radius;
                const y = node.y + Math.sin(dot.angle) * dot.radius;
                
                dot.element.attr('cx', x).attr('cy', y);
            });
        });
    },

    createOrbitDot: function (node) {
        // Keep original function for backward compatibility but it's now less used
        if (!this.trafficGroup || !node.x || !node.y) return;

        const radius = this.getNodeRadius(node) + 10;
        const angle = Math.random() * Math.PI * 2;
        const startX = node.x + Math.cos(angle) * radius;
        const startY = node.y + Math.sin(angle) * radius;

        const dot = this.trafficGroup.append('circle')
            .attr('class', 'orbit-dot')
            .attr('r', 3)
            .attr('fill', '#2196f3')
            .attr('opacity', 0.8)
            .attr('cx', startX)
            .attr('cy', startY);

        // Animate in orbit
        const duration = 3000 + Math.random() * 2000;

        dot.transition()
            .duration(duration)
            .ease(d3.easeLinear)
            .attrTween('cx', () => t => node.x + Math.cos(angle + t * Math.PI * 2) * radius)
            .attrTween('cy', () => t => node.y + Math.sin(angle + t * Math.PI * 2) * radius)
            .on('end', function () {
                d3.select(this).remove();
            });
    },

    truncateText: function (text, maxLength) {
        if (!text) return '';
        return text.length > maxLength ? text.substring(0, maxLength) + '...' : text;
    },

    resetView: function () {
        if (this.svg && this.zoom) {
            this.svg.transition()
                .duration(750)
                .call(this.zoom.transform, d3.zoomIdentity);
        }
    },

    enterFullScreen: function (containerId) {
        const element = document.getElementById(containerId);
        if (element) {
            if (element.requestFullscreen) {
                element.requestFullscreen();
            } else if (element.webkitRequestFullscreen) {
                element.webkitRequestFullscreen();
            } else if (element.msRequestFullscreen) {
                element.msRequestFullscreen();
            }
        }
    },

    exitFullScreen: function () {
        if (document.exitFullscreen) {
            document.exitFullscreen();
        } else if (document.webkitExitFullscreen) {
            document.webkitExitFullscreen();
        } else if (document.msExitFullscreen) {
            document.msExitFullscreen();
        }
    },

    resize: function () {
        if (!this.container || !this.svg) return;

        this.width = this.container.clientWidth;
        this.height = this.container.clientHeight || 600;

        this.svg.attr('viewBox', `0 0 ${this.width} ${this.height}`);

        if (this.simulation) {
            this.simulation.force('center', d3.forceCenter(this.width / 2, this.height / 2));
            this.simulation.alpha(0.1).restart();
        }
    },

    dispose: function () {
        if (this.animationInterval) {
            clearInterval(this.animationInterval);
            this.animationInterval = null;
        }

        if (this.orbitAnimationFrame) {
            cancelAnimationFrame(this.orbitAnimationFrame);
            this.orbitAnimationFrame = null;
        }

        // Clean up orbiting dots
        this.orbitingDots.forEach(dots => {
            dots.forEach(dot => {
                if (dot && dot.element) {
                    dot.element.remove();
                }
            });
        });
        this.orbitingDots.clear();

        if (this.simulation) {
            this.simulation.stop();
            this.simulation = null;
        }

        if (this.svg) {
            this.svg.remove();
            this.svg = null;
        }

        this.dotnetRef = null;
        this.nodes = [];
        this.links = [];
        this.highwayLinks = [];
    }
};
